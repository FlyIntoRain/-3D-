using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
// 导入ZXing库的命名空间
using ZXing;
using ZXing.QrCode;

public class QRCodeGenerator : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField]
    private RawImage qrCodeImage; // 将你场景中的Raw Image拖到这里

    [Header("网络配置")]
    [SerializeField]
    private int webSocketPort = 8888; // 你的WebSocket服务器端口
    [SerializeField]
    private int httpServerPort = 8000; // 你的网页服务器端口 (例如VS Code Live Server)

    void Start()
    {
        // 检查是否已分配UI组件
        if (qrCodeImage == null)
        {
            Debug.LogError("请将场景中的RawImage组件拖拽到脚本的qrCodeImage字段上！");
            return;
        }

        // 1. 获取本机的以太网IP地址 (不再是通用的LocalIPv4)
        string localIP = GetEthernetIP();

        // 2. 获取本机的局域网IP地址
        //string localIP = GetLocalIPv4();
        if (string.IsNullOrEmpty(localIP))
        {
            Debug.LogError("无法获取有效的本机IP地址！");
            return;
        }

        // 2. 构建手机需要访问的完整URL
        // 格式: http://[电脑IP]:[网页端口]/?ip=[电脑IP]
        string url = $"http://{localIP}:{httpServerPort}/?ip={localIP}";
        Debug.Log($"生成的URL为: {url}");

        // 3. 根据URL生成二维码纹理
        Texture2D qrCodeTexture = GenerateQRCode(url);

        // 4. 将生成的纹理应用到UI上
        qrCodeImage.texture = qrCodeTexture;
    }

    /// <summary>
    /// 获取本机的IPv4地址
    /// </summary>
    private string GetLocalIPv4()
    {
        // 获取所有网络接口
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in interfaces)
        {
            // 筛选：无线局域网类型（Wireless80211）且状态为已连接
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                ni.OperationalStatus == OperationalStatus.Up)
            {
                IPInterfaceProperties properties = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddr in properties.UnicastAddresses)
                {
                    // 筛选IPv4地址
                    if (unicastAddr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return unicastAddr.Address.ToString();
                    }
                }
            }
        }
        return string.Empty; // 未找到无线局域网IPv4地址时返回空
    }

    /// <summary>
    /// 使用ZXing.Net生成二维码
    /// </summary>
    /// <param name="text">要编码到二维码中的文本 (这里是URL)</param>
    /// <returns>包含二维码的Texture2D</returns>
    private Texture2D GenerateQRCode(string text)
    {
        // 1. 创建一个Texture2D来承载二维码
        var texture = new Texture2D(256, 256);

        // 2. 使用核心编码器将文本编码为BitMatrix
        // BitMatrix是二维码数据的逻辑表示 (0和1的矩阵)
        var writer = new QRCodeWriter();
        var bitMatrix = writer.encode(text, BarcodeFormat.QR_CODE, texture.width, texture.height);

        // 3. 将BitMatrix转换为Unity可以使用的颜色数组 (Color32[])
        var pixels = new Color32[bitMatrix.Width * bitMatrix.Height];
        for (int y = 0; y < bitMatrix.Height; y++)
        {
            for (int x = 0; x < bitMatrix.Width; x++)
            {
                // bitMatrix[x, y]为true代表黑色方块，false代表白色方块
                pixels[y * bitMatrix.Width + x] = bitMatrix[x, y] ? Color.black : Color.white;
            }
        }

        // 4. 将像素数据应用到Texture2D上
        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// 获取本机以太网（有线网络）的IPv4地址
    /// </summary>
    public string GetEthernetIP()
    {
        // 获取所有网络接口
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            // 筛选条件：
            // 1. 类型必须是 Ethernet (以太网)
            // 2. 状态必须是 Up (已连接)
            // 3. (可选) 排除描述中包含 "Virtual" 或 "VMware" 的虚拟网卡，防止获取到虚拟机的IP
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                ni.OperationalStatus == OperationalStatus.Up)
            {
                // 简单的防虚拟网卡过滤 (根据需要取消注释)
                // if (ni.Description.ToLower().Contains("virtual") || ni.Description.ToLower().Contains("vmware")) continue;

                // 获取该接口下的IP信息
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    // 确保是 IPv4 地址
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }

        return string.Empty; // 未找到符合条件的IP
    }
}
