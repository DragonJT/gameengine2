mod engine;

fn add_texture<'l>(engine:&mut engine::Engine<'l>, bytes:&'l [u8]) -> usize{
    let image = image::load_from_memory(bytes).unwrap();

    use image::GenericImageView;
    let rgba = image.to_rgba8();

    let dimensions = image.dimensions();
    let size = wgpu::Extent3d {
        width: dimensions.0,
        height: dimensions.1,
        depth_or_array_layers: 1,
    };
    engine.add_texture(rgba.into_vec(), size)
}

fn main() {
    let mut engine = engine::Engine::new(1200.0, 800.0);
    let happy_tree_texture = add_texture(&mut engine, include_bytes!("happy-tree.png"));
    let color_shader = engine.add_shader(wgpu::ShaderSource::Wgsl(include_str!("color_shader.wgsl").into()));
    let color_layout = engine.add_vertex_attributes(&[wgpu::VertexFormat::Float32x3, wgpu::VertexFormat::Float32x3]);

    let color_triangle_vertices:[f32;18] = [0.0, 0.5, 0.0, 1.0, 0.0, 0.0,  -0.5, -0.5, 0.0, 0.0, 1.0, 0.0,  0.5, -0.5, 0.0, 0.0, 0.0, 1.0];
    let color_triangle_indices:[u16;3] = [0,1,2];
    let color_triangle = engine.add_mesh(
        bytemuck::cast_slice(&color_triangle_vertices), 
        color_layout, 
        bytemuck::cast_slice(&color_triangle_indices), 
        wgpu::IndexFormat::Uint16,
        vec![],
    );
    engine.add_render_pass(color_shader, engine::LoadOp::Clear(0.3, 0.3, 0.3, 1.0), color_triangle);


    let tex_shader = engine.add_shader(wgpu::ShaderSource::Wgsl(include_str!("tex_shader.wgsl").into()));
    let tex_layout = engine.add_vertex_attributes(&[wgpu::VertexFormat::Float32x3, wgpu::VertexFormat::Float32x2]);

    let tex_triangle_vertices:[f32;15] = [0.0, 0.3, 0.0, 0.5, 1.0,  -0.3, -0.3, 0.0, 0.0, 0.0,  0.3, -0.3, 0.0, 1.0, 0.0];
    let tex_triangle_indices:[u16;3] = [0,1,2];
    let tex_triangle = engine.add_mesh(
        bytemuck::cast_slice(&tex_triangle_vertices), 
        tex_layout, 
        bytemuck::cast_slice(&tex_triangle_indices), 
        wgpu::IndexFormat::Uint16,
        vec![happy_tree_texture],
    );
    engine.add_render_pass(tex_shader, engine::LoadOp::Load, tex_triangle);

    engine.run();
}
