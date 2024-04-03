mod engine;

fn main() {
    let mut engine = engine::Engine::new(1200.0, 800.0);
    let shader = engine.add_shader(wgpu::ShaderSource::Wgsl(include_str!("shader.wgsl").into()));
    let layout = engine.add_vertex_attributes(&[wgpu::VertexFormat::Float32x3, wgpu::VertexFormat::Float32x3]);
    let triangle:[f32;18] = [0.0, 0.5, 0.0, 1.0, 0.0, 0.0,  -0.5, -0.5, 0.0, 0.0, 1.0, 0.0,  0.5, -0.5, 0.0, 0.0, 0.0, 1.0];
    let triangle_indices:[u16;3] = [0,1,2];
    let triangle_id = engine.add_mesh(
        bytemuck::cast_slice(&triangle), 
        layout, 
        bytemuck::cast_slice(&triangle_indices), 
        wgpu::IndexFormat::Uint16);
    engine.add_render_pass(shader, engine::LoadOp::Clear(0.3, 0.3, 0.3, 1.0), triangle_id);
    engine.run();
}
