using UnityEngine;
using System.Net;
using System.IO;
using System.Threading.Tasks; // 用于异步处理

public class EmbeddedHttpServer : MonoBehaviour
{
    private HttpListener listener;
    private int port = 8000; // 与你的QRCodeGenerator中设置的端口保持一致

    void Awake()
    {
        // 使用 DontDestroyOnLoad 可以确保服务器在切换场景时依然运行
        DontDestroyOnLoad(gameObject);

        // 启动服务器
        StartServer();
    }

    private void StartServer()
    {
        listener = new HttpListener();
        // 监听所有网络接口的指定端口
        // 这对于让手机能够访问至关重要！
        listener.Prefixes.Add($"http://*:{port}/");

        try
        {
            listener.Start();
            Debug.Log($"<color=green>内嵌HTTP服务器已在端口 {port} 上成功启动。</color>");

            // 异步监听传入的请求，避免阻塞Unity主线程
            Task.Run(() => ListenForRequests());
        }
        catch (HttpListenerException ex)
        {
            Debug.LogError($"<color=red>HTTP服务器启动失败: {ex.Message}</color>");
            Debug.LogWarning("提示: 可能是由于权限不足。在Windows上，可以尝试以管理员身份运行程序。");
        }
    }

    private async void ListenForRequests()
    {
        // 只要listener在运行，就持续接受请求
        while (listener.IsListening)
        {
            try
            {
                // 等待下一个请求
                HttpListenerContext context = await listener.GetContextAsync();
                // 处理这个请求
                ProcessRequest(context);
            }
            catch (HttpListenerException)
            {
                // 当服务器停止时，这里可能会抛出异常，可以安全地忽略
                break;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"请求处理中发生错误: {ex.Message}");
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        // 获取请求的本地路径
        string requestedPath = context.Request.Url.LocalPath;
        // 默认文件为 index.html
        if (requestedPath == "/")
        {
            requestedPath = "/index.html";
        }

        // 构建文件的完整物理路径
        string filePath = Path.Combine(Application.streamingAssetsPath, requestedPath.TrimStart('/'));

        if (File.Exists(filePath))
        {
            try
            {
                // 读取文件内容
                byte[] buffer = File.ReadAllBytes(filePath);

                // 发送HTTP响应
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取文件失败: {filePath}。错误: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
        }
        else
        {
            Debug.LogWarning($"文件未找到: {filePath}");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Close();
        }
    }

    // 确保在程序退出时关闭服务器，释放端口
    private void OnApplicationQuit()
    {
        if (listener != null && listener.IsListening)
        {
            Debug.Log("正在关闭HTTP服务器...");
            listener.Stop();
            listener.Close();
        }
    }
}
