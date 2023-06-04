using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Fresnel2D : MonoBehaviour
{
    [Header("[批处理图片组]")]
    public Texture2D[] tex;
    [Header("[轮廓方向]")]
    public SideType sideType = SideType.inSide;
    public enum SideType
    {
        inSide = 0,    //内轮廓
        outSide = 1,
    }
    [Header("[出图方式]")]
    public BakePlan bakePlan = BakePlan.BlackBack;
    public enum BakePlan
    {
        AlphaBack = 0,
        BlackBack = 1,
        //MergeRGBA = 2,    //将原图加入A通道
        MergeRGB = 3,     //将fresnel合并入原图
    }
    /*
    [Header("[出图缩放]")]
    public BakeScale bakeScale = BakeScale.x1;
    public enum BakeScale
    {
        x1 = 1,
        x05 = 2,
        x025 = 4,
        x0125 = 8,
    }*/
    [Header("[边缘范围]")]
    [Range(1,10)]
    public int range = 3;
    [Header("[边缘衰减]")]
    public bool attenuation = true;   //衰减 越远越透明
    [Range(0, 1)]
    [Header("[忽视半透度]")]
    public float alphaValue = 0.75f;   //如果图片透明度低于该值， 则视为无效
    [Header("[批处理进度]")]
    //public string texProgress = "";
    public string mainProgress = "";

    public string outPath = "Assets/Fresnel2D/TexMakeDemo/";
    string texName = "";
    GameObject pathObj;

    
    // Start is called before the first frame update
    float ttt = 0;
    void Awake()
    {
        ttt = Time.realtimeSinceStartup;
    }
    void Init()
    {
        //Object parentObject = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject);    //非运行状态下 使用编辑器功能获取到prefab
        if(pathObj != null)
        {
            GameObject parentObject = pathObj;
            string assetPaths = UnityEditor.AssetDatabase.GetAssetPath(parentObject);
            string[] strs = assetPaths.Split('/');
            outPath = "";
            for (int i = 0; i < strs.Length - 1; i++)
            {
                outPath += strs[i];
                outPath += "/";
            }
        }
        
    }
    Color backColor = Color.black;
    void Start()
    {
        Init();
        StartCoroutine("MakeFresnelTex");
    }
    Dictionary<string, int> checkedList = new Dictionary<string, int>();
    IEnumerator MakeFresnelTex()
    {
        for (int i = 0; i < tex.Length; i++)
        {
            mainProgress = string.Format("当前烘焙进度：{0}/{1}", i+ 1, tex.Length);

            switch (bakePlan)
            {
                case BakePlan.AlphaBack:   
                    backColor = new Color(0, 0, 0, 0);
                    texName = tex[i].name + "_side_alpha.png";
                    break;
                case BakePlan.BlackBack:    //外描边
                    backColor = new Color(0, 0, 0, 1);
                    texName = tex[i].name + "_side_black.png";
                    break;
                //case BakePlan.MergeRGBA:    //外描边
                    //backColor = new Color(0, 0, 0, 0);
                    //texName = tex[i].name + "_side_rgba.tga";
                    //break;
                case BakePlan.MergeRGB:    //外描边
                    backColor = new Color(0, 0, 0, 0);
                    texName = tex[i].name + "_side_rgb.png";
                    break;
            }

            Debug.Log(string.Format("  开始烘焙：      当前计时 {0} 秒", (Time.realtimeSinceStartup - ttt)));
            //texProgress = string.Format("  开始烘焙：      当前计时 {0} 秒", (Time.realtimeSinceStartup - ttt));
            Texture2D newTex = new Texture2D(tex[i].width, tex[i].height);
            int sec = 0;
            float per = 0;
            int max = tex[i].width * tex[i].height;
            //List<float> values = new List<float>();


            //List<string> forCheckList = new List<string>();
            List<string> tempCheckList = new List<string>();
            for (int h = 0; h < tex[i].height; h++)
            {
                for (int w = 0; w < tex[i].width; w++)
                {
                    sec++;
                    Color color = tex[i].GetPixel(w, h);
                    switch (sideType)
                    {
                        case SideType.inSide:    //内描边
                            if (color.a < alphaValue)
                            {
                                if (bakePlan == BakePlan.MergeRGB) newTex.SetPixel(w, h, tex[i].GetPixel(w, h));    // || bakePlan == BakePlan.MergeRGBA
                                else newTex.SetPixel(w, h, backColor);
                                continue;
                            }
                            break;
                        case SideType.outSide:    //外描边
                            if (color.a > alphaValue)
                            {
                                if (bakePlan == BakePlan.MergeRGB) newTex.SetPixel(w, h, tex[i].GetPixel(w, h));      // || bakePlan == BakePlan.MergeRGBA
                                else newTex.SetPixel(w, h, backColor);
                                continue;
                            }
                            break;
                    }

                    //forCheckList.Clear();
                    //tempCheckList.Clear();
                    checkedList.Clear();
                    //values.Clear();

                    //这里便利周边n层像素，有没有符合条件的
                    tempCheckList.Clear();
                    tempCheckList.Add(string.Format("{0},{1}", w - 1, h));
                    tempCheckList.Add(string.Format("{0},{1}", w + 1, h));
                    tempCheckList.Add(string.Format("{0},{1}", w, h - 1));
                    tempCheckList.Add(string.Format("{0},{1}", w, h + 1));
                    bool find = false;
                    int dis = 0;

                    bool side4 = false;
                    for (int k = 0; k < range; k++)    //这里每个子件要遍历
                    {
                        List<string> forCheckList = new List<string>(tempCheckList);
                        tempCheckList.Clear();
                        for (int j = 0; j < forCheckList.Count; j++)
                        {
                            string[] strs = forCheckList[j].Split(',');

                            Vector2 uv = new Vector2(int.Parse(strs[0]) - 1, int.Parse(strs[1]));
                            float value_1 = GetSideValue(uv, tex[i]);
                            if (value_1 != -1) tempCheckList.Add(string.Format("{0},{1}", uv.x, uv.y));

                            uv = new Vector2(int.Parse(strs[0]) + 1, int.Parse(strs[1]));
                            float value_2 = GetSideValue(uv, tex[i]);
                            if (value_2 != -1) tempCheckList.Add(string.Format("{0},{1}", uv.x, uv.y));

                            uv = new Vector2(int.Parse(strs[0]), int.Parse(strs[1]) - 1);
                            float value_3 = GetSideValue(uv, tex[i]);
                            if (value_3 != -1) tempCheckList.Add(string.Format("{0},{1}", uv.x, uv.y));

                            uv = new Vector2(int.Parse(strs[0]), int.Parse(strs[1]) + 1);
                            float value_4 = GetSideValue(uv, tex[i]);
                            if (value_4 != -1) tempCheckList.Add(string.Format("{0},{1}", uv.x, uv.y));

                            float value = Mathf.Max(value_1, value_2);
                            value = Mathf.Max(value, value_3);
                            value = Mathf.Max(value, value_4);
                            //value -1无效  0不增长   1增长
                            //values.Add(value);

                            if(sideType == SideType.outSide)
                            {
                                uv = new Vector2(int.Parse(strs[0]) - 1, int.Parse(strs[1]) - 1);
                                float value_5 = GetSideValueSimple(uv, tex[i]);

                                uv = new Vector2(int.Parse(strs[0]) + 1, int.Parse(strs[1]) - 1);
                                float value_6 = GetSideValueSimple(uv, tex[i]);

                                uv = new Vector2(int.Parse(strs[0]) - 1, int.Parse(strs[1]) + 1);
                                float value_7 = GetSideValueSimple(uv, tex[i]);

                                uv = new Vector2(int.Parse(strs[0]) + 1, int.Parse(strs[1]) + 1);
                                float value_8 = GetSideValueSimple(uv, tex[i]);

                                float value2 = Mathf.Max(value_5, value_6);
                                value2 = Mathf.Max(value2, value_7);
                                value2 = Mathf.Max(value2, value_8);
                                if (value2 == 1)
                                {
                                    side4 = true;
                                }
                            }

                            if (value == 1)
                            {
                                find = true;
                                break;    //如果有一层有价值， 则当前网格就是高价值网格
                            }
                        }

                        if (!find) dis++;
                    }
                    if (dis == range)  //达到边界了 有没有符合条件的
                    {
                        if (!side4)
                        {
                            if (bakePlan == BakePlan.MergeRGB) newTex.SetPixel(w, h, tex[i].GetPixel(w, h));    // || bakePlan == BakePlan.MergeRGBA
                            else newTex.SetPixel(w, h, backColor);
                            continue;
                        }
                        
                    }
                    float v = 1;
                    if (attenuation)   //衰减
                    {
                        v = (float)(range - dis) / (float)range;
                        if (sideType == SideType.outSide)
                        {
                            v *= 0.95f;
                        }
                    }
                    if (side4)
                    {
                        //v = Mathf.Clamp(v + (0.5f/range), 0, 1);
                    }
                    switch (bakePlan)
                    {
                        case BakePlan.AlphaBack:
                            newTex.SetPixel(w, h, new Color(1, 1, 1, v));
                            break;
                        case BakePlan.BlackBack:    
                            Color c = Color.white * v;
                            c.a = 1;
                            newTex.SetPixel(w, h, c);
                            break;
                        //case BakePlan.MergeRGBA:
                            //c = tex[i].GetPixel(w, h);
                            //c.a = v;
                            //newTex.SetPixel(w, h, c);
                            //break;
                        case BakePlan.MergeRGB:
                            c = tex[i].GetPixel(w, h);
                            c = Color.Lerp(c, new Color(1, 1, 1, 1), v);
                            newTex.SetPixel(w, h, c);
                            break;
                    }
                    

                    if (sec > 1000)     //数值越短，进度条刷新越快  20000   -   50000
                    {
                        per += sec;
                        sec = 0;
                        Debug.Log(string.Format("           已完成： {0} / {1}   (pixels)                百分比：  {2} %", per, max, (per / max * 100)));   // + "   耗时 " + ((Time.realtimeSinceStartup - ttt) / 60 )
                                                                                                                                                    //if (per > 512) yield return null;
                        yield return new WaitForSeconds(0.001f);
                    }
                }
            }
            newTex.Apply(false);

            yield return new WaitForSeconds(0.001f);
            Debug.Log(string.Format("    图片烘焙完成, 花费 {0} 分", ((Time.realtimeSinceStartup - ttt) / 60)));
            //yield return new WaitForSeconds(0.1f);
            string url = outPath + texName;
            WriteTexture(newTex, url);
            yield return new WaitForSeconds(0.001f);
            Debug.Log(string.Format("    烘焙图创建完成, 共花费 {0} 分", ((Time.realtimeSinceStartup - ttt) / 60)));
            //yield return new WaitForSeconds(0.1f);
        }
    }
    float GetSideValue(Vector2 uv, Texture2D tex)
    {
        if(checkedList.ContainsKey(string.Format("{0},{1}",uv.x, uv.y))) return -1;
        if (uv.x < 0 || uv.x > tex.width - 1) return -1;
        if (uv.y < 0 || uv.y > tex.height - 1) return -1;

        checkedList.Add(string.Format("{0},{1}", uv.x, uv.y), 0);

        Color color = tex.GetPixel(Mathf.RoundToInt(uv.x), Mathf.RoundToInt(uv.y));
        switch (sideType)
        {
            case SideType.inSide:
                if (color.a < alphaValue) return 1;
                else return 0;
            case SideType.outSide:
                if (color.a < alphaValue) return 0;
                else return 1;
        }
        return -1;
    }
    float GetSideValueSimple(Vector2 uv, Texture2D tex)
    {
        if (checkedList.ContainsKey(string.Format("{0},{1}", uv.x, uv.y))) return -1;
        if (uv.x < 0 || uv.x > tex.width - 1) return -1;
        if (uv.y < 0 || uv.y > tex.height - 1) return -1;

        Color color = tex.GetPixel(Mathf.RoundToInt(uv.x), Mathf.RoundToInt(uv.y));
        switch (sideType)
        {
            case SideType.inSide:
                if (color.a < alphaValue) return 1;
                else return 0;
            case SideType.outSide:
                if (color.a < alphaValue) return 0;
                else return 1;
        }
        return -1;
    }
    void WriteTexture(Texture2D ttt, string url)
    {
        byte[] bytes;
        //if (bakePlan == BakePlan.MergeRGBA) bytes = ttt.EncodeToTGA();
        //else
            bytes = ttt.EncodeToPNG();
        //File.WriteAllBytes(url);//保存

        FileStream file = File.Open(url, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();

        UnityEditor.AssetDatabase.Refresh();
    }
}
