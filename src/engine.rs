use wgpu::util::DeviceExt;

pub struct Engine<'l>{
    window_width:f64,
    window_height:f64,
    textures:Vec<Texture>,
    shaders:Vec<wgpu::ShaderSource<'l>>,
    vertex_attributes:Vec<&'l[wgpu::VertexFormat]>,
    meshes:Vec<Mesh<'l>>,
    render_passes:Vec<RenderPass>,
}

pub enum LoadOp{
    Clear(f64,f64,f64,f64),
    Load,
}

struct Texture{
    data:Vec<u8>,
    size:wgpu::Extent3d,
}

struct TextureData{
    bind_group_layout:wgpu::BindGroupLayout,
    bind_group:wgpu::BindGroup,
}

struct Mesh<'l>{
    vertex_data:&'l[u8],
    vertex_attribtes_id:usize,
    index_data:&'l[u8],
    index_format:wgpu::IndexFormat,
    texture_ids:Vec<usize>,
}

struct VertexAttributeData{
    stride:u64,
    attributes:Vec<wgpu::VertexAttribute>,
}

struct MeshData{
    vertex_buffer:wgpu::Buffer,
    index_buffer:wgpu::Buffer,
    num_indices:u32,
}

struct RenderPass{
    shader_id:usize,
    load_op:LoadOp,
    mesh_id:usize,
}

impl<'l> Engine<'l>{

    pub fn new(window_width:f64, window_height:f64)->Engine<'l>{
        Engine { 
            window_width, 
            window_height, 
            textures: Vec::new(),
            shaders: Vec::new(), 
            meshes:Vec::new(), 
            render_passes: Vec::new(), 
            vertex_attributes:Vec::new() 
        }
    }

    pub fn add_texture(&mut self, data:Vec<u8>, size:wgpu::Extent3d)->usize{
        self.textures.push(Texture { data, size });
        self.textures.len() - 1
    }

    pub fn add_vertex_attributes(&mut self, vertex_attributes:&'l[wgpu::VertexFormat]) -> usize{
        self.vertex_attributes.push(vertex_attributes);
        self.vertex_attributes.len() - 1
    }

    pub fn add_shader(&mut self, shader:wgpu::ShaderSource<'l>) -> usize{
        self.shaders.push(shader);
        self.shaders.len() - 1
    }

    pub fn add_render_pass(&mut self, shader_id:usize, load_op:LoadOp, mesh_id:usize){
        self.render_passes.push( RenderPass { shader_id, load_op, mesh_id });
    }

    pub fn add_mesh(
        &mut self, 
        vertex_data:&'l[u8], 
        vertex_buffer_layout_id:usize, 
        index_data:&'l[u8], 
        index_format:wgpu::IndexFormat,
        texture_ids:Vec<usize>,
    ) -> usize{
        self.meshes.push(Mesh { vertex_data, vertex_attribtes_id: vertex_buffer_layout_id, index_data, index_format, texture_ids });
        self.meshes.len() - 1
    }

    fn get_index_format_size(format:&wgpu::IndexFormat)->u32{
        match format{
            wgpu::IndexFormat::Uint16 => 2,
            wgpu::IndexFormat::Uint32 => 4,
        }
    }

    fn get_vertex_format_size(format:&wgpu::VertexFormat)->u64{
        match format{
            wgpu::VertexFormat::Float32 => 4,
            wgpu::VertexFormat::Float32x2 => 4*2,
            wgpu::VertexFormat::Float32x3 => 4*3,
            wgpu::VertexFormat::Float32x4 => 4*4,
            _=>panic!("fill out vertex format size here"),
        }
    }

    pub fn run(self) {
        env_logger::init();
        let event_loop = winit::event_loop::EventLoop::new().unwrap();
        let window = winit::window::WindowBuilder::new()
            .with_position(winit::dpi::Position::Logical(winit::dpi::LogicalPosition{x:25.0, y:25.0}))
            .with_inner_size(winit::dpi::Size::Logical(winit::dpi::LogicalSize{width:self.window_width, height:self.window_height}))
            .build(&event_loop)
            .unwrap();
    
        let size = window.inner_size();

        let instance = wgpu::Instance::new(wgpu::InstanceDescriptor {
            backends: wgpu::Backends::GL,
            ..Default::default()
        });
        
        let surface = instance.create_surface(&window).unwrap();

        let adapter = futures::executor::block_on(instance.request_adapter(
            &wgpu::RequestAdapterOptions {
                power_preference: wgpu::PowerPreference::default(),
                compatible_surface: Some(&surface),
                force_fallback_adapter: false,
            },
        )).unwrap();

        let (device, queue) =  futures::executor::block_on(adapter.request_device(
            &wgpu::DeviceDescriptor {
                required_features: wgpu::Features::empty(),
                required_limits:  wgpu::Limits::default(),
                label: None,
            },
            None,
        )).unwrap();

        let surface_caps = surface.get_capabilities(&adapter);
        let surface_format = surface_caps.formats.iter()
            .copied()
            .filter(|f| f.is_srgb())
            .next()
            .unwrap_or(surface_caps.formats[0]);
        let mut config = wgpu::SurfaceConfiguration {
            usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
            format: surface_format,
            width: size.width,
            height: size.height,
            present_mode: surface_caps.present_modes[0],
            alpha_mode: surface_caps.alpha_modes[0],
            view_formats: vec![],
            desired_maximum_frame_latency:1, 
        };
        surface.configure(&device, &config);

        //======================================================================

        let mut textures:Vec<TextureData> = Vec::new();
        for texture in self.textures{
            let wgpu_texture = device.create_texture(
                &wgpu::TextureDescriptor {
                    size: texture.size,
                    mip_level_count: 1, 
                    sample_count: 1,
                    dimension: wgpu::TextureDimension::D2,
                    format: wgpu::TextureFormat::Rgba8UnormSrgb,
                    usage: wgpu::TextureUsages::TEXTURE_BINDING | wgpu::TextureUsages::COPY_DST,
                    label: Some("diffuse_texture"),
                    view_formats: &[],
                }
            );

            queue.write_texture(
                wgpu::ImageCopyTexture {
                    texture: &wgpu_texture,
                    mip_level: 0,
                    origin: wgpu::Origin3d::ZERO,
                    aspect: wgpu::TextureAspect::All,
                },
                &texture.data,
                wgpu::ImageDataLayout {
                    offset: 0,
                    bytes_per_row: Some(4 * texture.size.width),
                    rows_per_image: Some(texture.size.height),
                },
                texture.size,
            );

            let view = wgpu_texture.create_view(&wgpu::TextureViewDescriptor::default());
            let sampler = device.create_sampler(&wgpu::SamplerDescriptor {
                address_mode_u: wgpu::AddressMode::ClampToEdge,
                address_mode_v: wgpu::AddressMode::ClampToEdge,
                address_mode_w: wgpu::AddressMode::ClampToEdge,
                mag_filter: wgpu::FilterMode::Linear,
                min_filter: wgpu::FilterMode::Nearest,
                mipmap_filter: wgpu::FilterMode::Nearest,
                ..Default::default()
            });

            let bind_group_layout = device.create_bind_group_layout(&wgpu::BindGroupLayoutDescriptor {
                entries: &[
                    wgpu::BindGroupLayoutEntry {
                        binding: 0,
                        visibility: wgpu::ShaderStages::FRAGMENT,
                        ty: wgpu::BindingType::Texture {
                            multisampled: false,
                            view_dimension: wgpu::TextureViewDimension::D2,
                            sample_type: wgpu::TextureSampleType::Float { filterable: true },
                        },
                        count: None,
                    },
                    wgpu::BindGroupLayoutEntry {
                        binding: 1,
                        visibility: wgpu::ShaderStages::FRAGMENT,
                        ty: wgpu::BindingType::Sampler(wgpu::SamplerBindingType::Filtering),
                        count: None,
                    },
                ],
                label: Some("texture_bind_group_layout"),
            });

            let bind_group = device.create_bind_group(
                &wgpu::BindGroupDescriptor {
                    layout: &bind_group_layout,
                    entries: &[
                        wgpu::BindGroupEntry {
                            binding: 0,
                            resource: wgpu::BindingResource::TextureView(&view),
                        },
                        wgpu::BindGroupEntry {
                            binding: 1,
                            resource: wgpu::BindingResource::Sampler(&sampler),
                        }
                    ],
                    label: Some("diffuse_bind_group"),
                }
            );
            textures.push(TextureData { bind_group_layout, bind_group });
        }

        let mut shaders:Vec<wgpu::ShaderModule> = Vec::new();
        for shader in self.shaders{
            shaders.push(device.create_shader_module(wgpu::ShaderModuleDescriptor {
                label: Some("Shader"),
                source: shader,
            }));
        }

        let mut vertex_attribute_datas:Vec<VertexAttributeData> = Vec::new();

        for vertex_attributes in self.vertex_attributes{
            let mut location = 0;
            let mut stride = 0;
            let mut attributes:Vec<wgpu::VertexAttribute> = Vec::new();
            for format in vertex_attributes{
                attributes.push(wgpu::VertexAttribute{offset:stride, shader_location:location, format:*format});
                stride+=Self::get_vertex_format_size(format);
                location+=1;
            }
            vertex_attribute_datas.push(VertexAttributeData { stride, attributes });
        }

        let mut meshes:Vec<MeshData> = Vec::new();
        for mesh in &self.meshes{
            let vertex_buffer = device.create_buffer_init(
                &wgpu::util::BufferInitDescriptor {
                    label: Some("Vertex Buffer"),
                    contents: mesh.vertex_data,
                    usage: wgpu::BufferUsages::VERTEX,
                }
            );

            let index_buffer = device.create_buffer_init(
                &wgpu::util::BufferInitDescriptor {
                    label: Some("Index Buffer"),
                    contents: mesh.index_data,
                    usage: wgpu::BufferUsages::INDEX,
                }
            );

            let num_indices = mesh.index_data.len() as u32 / Self::get_index_format_size(&mesh.index_format);
            meshes.push(MeshData { vertex_buffer, index_buffer, num_indices });
        }

        let mut render_pipelines:Vec<wgpu::RenderPipeline> = Vec::new();
        for render_pass in &self.render_passes{
            let shader = &shaders[render_pass.shader_id];

            let mut bind_group_layouts:Vec<&wgpu::BindGroupLayout> = Vec::new();
            for texture_id in &self.meshes[render_pass.mesh_id].texture_ids{
                bind_group_layouts.push(&textures[*texture_id].bind_group_layout);
            }
            let render_pipeline_layout = device.create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
                label: Some("Render Pipeline Layout"),
                bind_group_layouts: &bind_group_layouts,
                push_constant_ranges: &[],
            });

            let vertex_attributes_data = &vertex_attribute_datas[self.meshes[render_pass.mesh_id].vertex_attribtes_id];
            let vertex_buffer_layout = wgpu::VertexBufferLayout {
                array_stride: vertex_attributes_data.stride,
                step_mode: wgpu::VertexStepMode::Vertex,
                attributes: &vertex_attributes_data.attributes,
            };  

            let render_pipeline = device.create_render_pipeline(&wgpu::RenderPipelineDescriptor {
                label: Some("Render Pipeline"),
                layout: Some(&render_pipeline_layout),
                vertex: wgpu::VertexState {
                    module: &shader,
                    entry_point: "vs_main",
                    buffers: &[vertex_buffer_layout],
                },
                fragment: Some(wgpu::FragmentState { // 3.
                    module: &shader,
                    entry_point: "fs_main",
                    targets: &[Some(wgpu::ColorTargetState { // 4.
                        format: config.format,
                        blend: Some(wgpu::BlendState::REPLACE),
                        write_mask: wgpu::ColorWrites::ALL,
                    })],
                }),
                primitive: wgpu::PrimitiveState {
                    topology: wgpu::PrimitiveTopology::TriangleList, // 1.
                    strip_index_format: None,
                    front_face: wgpu::FrontFace::Ccw,
                    cull_mode: Some(wgpu::Face::Back),
                    polygon_mode: wgpu::PolygonMode::Fill,
                    unclipped_depth: false,
                    conservative: false,
                },
                depth_stencil: None, // 1.
                multisample: wgpu::MultisampleState {
                    count: 1,
                    mask: !0,
                    alpha_to_coverage_enabled: false,
                },
                multiview: None,
            });

            render_pipelines.push(render_pipeline);
        }

        //======================================================================

        event_loop.run(|event, target| {
            match event{
                winit::event::Event::WindowEvent { window_id, event }=>{
                    if window.id() == window_id{
                        match event {
                            winit::event::WindowEvent::Resized(new_size)=>{
                                config.width = new_size.width;
                                config.height = new_size.height;
                                surface.configure(&device, &config);
                            }
                            winit::event::WindowEvent::RedrawRequested =>{ 
                                let output = surface.get_current_texture().unwrap();
                                let view = output.texture.create_view(&wgpu::TextureViewDescriptor::default());
                                let mut encoder = device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
                                    label: Some("Render Encoder"),
                                });
                                {
                                    for i in 0..self.render_passes.len(){
                                        let load_op = match self.render_passes[i].load_op{
                                            LoadOp::Clear(r,g,b,a)=>wgpu::LoadOp::Clear(wgpu::Color {r, g, b, a}),
                                            LoadOp::Load => wgpu::LoadOp::Load,
                                        };
                                        let mut render_pass = encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
                                            label: Some("Render Pass"),
                                            color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                                                view: &view,
                                                resolve_target: None,
                                                ops: wgpu::Operations {
                                                    load: load_op,
                                                    store: wgpu::StoreOp::Store,
                                                },
                                            })],
                                            depth_stencil_attachment: None,
                                            occlusion_query_set: None,
                                            timestamp_writes: None,
                                        });
                                        render_pass.set_pipeline(&render_pipelines[i]);

                                        let texture_ids = &self.meshes[self.render_passes[i].mesh_id].texture_ids;
                                        for i in 0..texture_ids.len(){
                                            render_pass.set_bind_group(i as u32, &textures[texture_ids[i]].bind_group, &[]);
                                        }

                                        let mesh_data = &meshes[self.render_passes[i].mesh_id];
                                        render_pass.set_vertex_buffer(0, mesh_data.vertex_buffer.slice(..));
                                        render_pass.set_index_buffer(mesh_data.index_buffer.slice(..), wgpu::IndexFormat::Uint16);
                                        render_pass.draw_indexed(0..mesh_data.num_indices, 0, 0..1);
                                    }
                                }
                                queue.submit(std::iter::once(encoder.finish()));
                                output.present();
                            
                            }
                            winit::event::WindowEvent::CloseRequested => target.exit(),
                            _=>{
    
                            }
                        }
                    }
                }
                _=>{}
            }
        }).unwrap();      
    }
}

