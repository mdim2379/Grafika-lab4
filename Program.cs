using System.Drawing;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Lab4;

internal class Program
{
    private const string fileName = "teapot.obj";

    private const string ModelMatrixVariableName = "uModel";
    private const string NormalMatrixVariableName = "uNormal";
    private const string ViewMatrixVariableName = "uView";
    private const string ProjectionMatrixVariableName = "uProjection";

    private const string LightColorVariableName = "lightColor";
    private const string LightPositionVariableName = "lightPos";
    private const string ViewPosVariableName = "viewPos";
    private const string ShininessVariableName = "shininess";
    private static readonly CameraDescriptor cameraDescriptor = new();

    private static readonly CubeArrangementModel cubeArrangementModel = new();

    private static IWindow graphicWindow;
    private static ImGuiController imGuiController;

    private static GL Gl;

    private static uint program;

    private static GlObject glTeapot;

    private static float Shininess = 40;

    private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            outNormal = uNormal*vNormal;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

    private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;

        uniform float shininess;
		
		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 1;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;
            
            float specularStrength = 0.1;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            
            float spec = sign(max(dot(norm, lightDir),0)) * pow( max(dot(viewDir, reflectDir), 0.0), shininess) / max(max(dot(norm,viewDir), 0), max(dot(norm,lightDir), 0));
            vec3 specular = specularStrength * spec * lightColor;  
            
            vec3 result = (ambient + diffuse + specular) * outCol.rgb;

            FragColor = vec4(result, outCol.w);
        }
        ";

    private static ModelObjectDescriptor kocka;

    private static void Main(string[] args)
    {
        var windowOptions = WindowOptions.Default;
        windowOptions.Size = new Vector2D<int>(500, 500);

        graphicWindow = Window.Create(windowOptions);
        graphicWindow.FramebufferResize += newSize => { Gl.Viewport(newSize); };

        graphicWindow.Load += GraphicWindow_Load;
        graphicWindow.Update += GraphicWindow_Update;
        graphicWindow.Render += GraphicWindow_Render;
        graphicWindow.Closing += GraphicWindow_Closing;

        graphicWindow.Run();
    }

    private static void GraphicWindow_Load()
    {
        Gl = graphicWindow.CreateOpenGL();

        var inputContext = graphicWindow.CreateInput();
        foreach (var keyboard in inputContext.Keyboards) keyboard.KeyDown += Keyboard_KeyDown;

        imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

        Gl.ClearColor(Color.White);

        Gl.Enable(EnableCap.CullFace);
        Gl.CullFace(GLEnum.Front);

        Gl.Enable(EnableCap.DepthTest);
        Gl.DepthFunc(DepthFunction.Lequal);

        glTeapot = ObjectResourceReader.CreateObjectFromResource(Gl, fileName);
        kocka = ModelObjectDescriptor.CreateBronzeCube(Gl);

        LinkProgram();
    }

    private static void LinkProgram()
    {
        var vshader = Gl.CreateShader(ShaderType.VertexShader);
        var fshader = Gl.CreateShader(ShaderType.FragmentShader);

        Gl.ShaderSource(vshader, VertexShaderSource);
        Gl.CompileShader(vshader);
        Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

        Gl.ShaderSource(fshader, FragmentShaderSource);
        Gl.CompileShader(fshader);

        program = Gl.CreateProgram();
        Gl.AttachShader(program, vshader);
        Gl.AttachShader(program, fshader);
        Gl.LinkProgram(program);
        Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0) Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");

        Gl.DetachShader(program, vshader);
        Gl.DetachShader(program, fshader);
        Gl.DeleteShader(vshader);
        Gl.DeleteShader(fshader);
        if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
        {
        }
    }

    private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        switch (key)
        {
            case Key.Left:
                cameraDescriptor.IncreaseZYAngle();
                break;
            case Key.Right:
                cameraDescriptor.DecreaseZYAngle();
                break;
            case Key.Up:
                cameraDescriptor.IncreaseZXAngle();
                break;
            case Key.Down:
                cameraDescriptor.DecreaseZXAngle();
                break;
            case Key.W:
                cameraDescriptor.setOffset(0);
                break;
            case Key.A:
                cameraDescriptor.setOffset(1);
                break;
            case Key.S:
                cameraDescriptor.setOffset(2);
                break;
            case Key.D:
                cameraDescriptor.setOffset(3);
                break;
            case Key.Space:
                cameraDescriptor.setOffset(4);
                break;
            case Key.ControlLeft:
                cameraDescriptor.setOffset(5);
                break;
        }
    }


    private static void GraphicWindow_Update(double deltaTime)
    {
        // no GL
        // not threadsafe
        cubeArrangementModel.AdvanceTime(deltaTime);
        imGuiController.Update((float)deltaTime);
    }

    private static void GraphicWindow_Render(double deltaTime)
    {
        // GL
        Gl.Clear(ClearBufferMask.ColorBufferBit);
        Gl.Clear(ClearBufferMask.DepthBufferBit);

        Gl.UseProgram(program);

        SetViewMatrix();
        SetProjectionMatrix();

        SetLight();

        var location = Gl.GetUniformLocation(program, ViewPosVariableName);
        if (location == -1) throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
        Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
        CheckError();

        SetShininess();

        DrawCenteredPulsingTeapot();
        DrawCube();

        //ImGuiNET.ImGui.ShowDemoWindow();
        ImGui.Begin("Lighting",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
        ImGui.End();

        imGuiController.Render();
    }

    private static void SetShininess()
    {
        var location = Gl.GetUniformLocation(program, ShininessVariableName);

        if (location == -1) throw new Exception($"{ShininessVariableName} uniform not found on shader.");

        // full white light
        Gl.Uniform1(location, Shininess);
        CheckError();
    }

    private static void SetLight()
    {
        var location = Gl.GetUniformLocation(program, LightColorVariableName);
        if (location == -1) throw new Exception($"{LightColorVariableName} uniform not found on shader.");
        // full white light
        Gl.Uniform3(location, 1f, 1f, 1f);
        CheckError();

        location = Gl.GetUniformLocation(program, LightPositionVariableName);
        if (location == -1) throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
        Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
        CheckError();
    }

    private static unsafe void DrawCenteredPulsingTeapot()
    {
        Gl.BindVertexArray(glTeapot.Vao);
        var ScaleVal = 0.01f;
        var modelMatrixForCenterCube = Matrix4X4.CreateScale(ScaleVal, ScaleVal, ScaleVal);
        var rotationMatrix = Matrix4X4.CreateRotationX(0f);
        SetModelMatrix(modelMatrixForCenterCube * rotationMatrix);
        Gl.DrawElements(PrimitiveType.Triangles, glTeapot.IndexArrayLength, DrawElementsType.UnsignedInt, null);
        Gl.BindVertexArray(0);
    }

    private static unsafe void DrawCube()
    {
        Gl.BindVertexArray(kocka.Vao);
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, kocka.Indices);
        var ScaleVal = 1f;
        var modelMatrixForCenterCube = Matrix4X4.CreateScale(ScaleVal, ScaleVal, ScaleVal);
        var rotationMatrix = Matrix4X4.CreateRotationX(0f);
        var moveMatrix = Matrix4X4.CreateTranslation(5f, 0f, 0f);
        SetModelMatrix(modelMatrixForCenterCube * rotationMatrix * moveMatrix);
        Gl.DrawElements(PrimitiveType.Triangles, kocka.IndexArrayLength, DrawElementsType.UnsignedInt, null);
        Gl.BindVertexArray(0);
    }

    private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
    {
        var location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
        if (location == -1) throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
        Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
        CheckError();

        // G = (M^-1)^T
        var modelMatrixWithoutTranslation =
            new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
        modelMatrixWithoutTranslation.M41 = 0;
        modelMatrixWithoutTranslation.M42 = 0;
        modelMatrixWithoutTranslation.M43 = 0;
        modelMatrixWithoutTranslation.M44 = 1;

        Matrix4X4<float> modelInvers;
        Matrix4X4.Invert(modelMatrixWithoutTranslation, out modelInvers);
        var normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
        location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
        if (location == -1) throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
        Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
        CheckError();
    }

    private static void GraphicWindow_Closing()
    {
        glTeapot.Release();
    }

    private static unsafe void SetProjectionMatrix()
    {
        var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView((float)(Math.PI / 2), 1024f / 768f, 0.1f, 100);
        var location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

        if (location == -1) throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");

        Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
        CheckError();
    }

    private static unsafe void SetViewMatrix()
    {
        var viewMatrix =
            Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
        var location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

        if (location == -1) throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");

        Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
        CheckError();
    }

    public static void CheckError()
    {
        var error = (ErrorCode)Gl.GetError();
        if (error != ErrorCode.NoError)
            throw new Exception("GL.GetError() returned " + error);
    }
}