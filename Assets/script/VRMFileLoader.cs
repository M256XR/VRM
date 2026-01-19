using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using VRM;
// using NativeFilePicker; ← この行を削除

public class VRMFileLoader : MonoBehaviour
{
    private GameObject currentVRM;
    private string statusText = "VRMファイルを選択してください";

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 200), statusText);
        
        // ファイル選択ボタン
        if (GUI.Button(new Rect(10, 220, 200, 60), "VRMファイルを選択"))
        {
            PickVRMFile();
        }
    }

    void PickVRMFile()
    {
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile(
            (path) =>
            {
                if (path != null)
                {
                    statusText = $"選択: {Path.GetFileName(path)}";
                    LoadVRMFromPath(path);
                }
                else
                {
                    statusText = "キャンセルされました";
                }
            },
            new string[] { ".vrm" }
        );

        if (permission == NativeFilePicker.Permission.Denied)
        {
            statusText = "権限が拒否されました";
        }
    }

    async void LoadVRMFromPath(string filePath)
    {
        statusText = "読み込み中...";
        
        if (!File.Exists(filePath))
        {
            statusText = "ファイルが見つかりません";
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            statusText = $"VRM解析中... ({bytes.Length / 1024 / 1024}MB)";
            
            var gltfData = new GlbLowLevelParser(filePath, bytes).Parse();
            var vrmData = new VRMData(gltfData);
            
            using (var context = new VRMImporterContext(vrmData))
            {
                var loaded = await context.LoadAsync(new RuntimeOnlyAwaitCaller());
                
                // 古いVRMを削除
                if (currentVRM != null)
                {
                    Destroy(currentVRM);
                }
                
                currentVRM = loaded.Root;
                currentVRM.transform.SetParent(transform);
                currentVRM.transform.localPosition = Vector3.zero;
                currentVRM.transform.localRotation = Quaternion.identity;
                
                statusText = "VRM読み込み成功！";
                Debug.Log("VRM読み込み成功！");
            }
        }
        catch (System.Exception e)
        {
            statusText = $"エラー: {e.Message}";
            Debug.LogError($"VRM読み込みエラー: {e.Message}");
        }
    }
}