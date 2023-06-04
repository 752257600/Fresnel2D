using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Fresnel2D : MonoBehaviour
{
    [Header("[������ͼƬ��]")]
    public Texture2D[] tex;
    [Header("[��������]")]
    public SideType sideType = SideType.inSide;
    public enum SideType
    {
        inSide = 0,    //������
        outSide = 1,
    }
    [Header("[��ͼ��ʽ]")]
    public BakePlan bakePlan = BakePlan.BlackBack;
    public enum BakePlan
    {
        AlphaBack = 0,
        BlackBack = 1,
        //MergeRGBA = 2,    //��ԭͼ����Aͨ��
        MergeRGB = 3,     //��fresnel�ϲ���ԭͼ
    }
    /*
    [Header("[��ͼ����]")]
    public BakeScale bakeScale = BakeScale.x1;
    public enum BakeScale
    {
        x1 = 1,
        x05 = 2,
        x025 = 4,
        x0125 = 8,
    }*/
    [Header("[��Ե��Χ]")]
    [Range(1,10)]
    public int range = 3;
    [Header("[��Ե˥��]")]
    public bool attenuation = true;   //˥�� ԽԶԽ͸��
    [Range(0, 1)]
    [Header("[���Ӱ�͸��]")]
    public float alphaValue = 0.75f;   //���ͼƬ͸���ȵ��ڸ�ֵ�� ����Ϊ��Ч
    [Header("[���������]")]
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
        //Object parentObject = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject);    //������״̬�� ʹ�ñ༭�����ܻ�ȡ��prefab
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
            mainProgress = string.Format("��ǰ�決���ȣ�{0}/{1}", i+ 1, tex.Length);

            switch (bakePlan)
            {
                case BakePlan.AlphaBack:   
                    backColor = new Color(0, 0, 0, 0);
                    texName = tex[i].name + "_side_alpha.png";
                    break;
                case BakePlan.BlackBack:    //�����
                    backColor = new Color(0, 0, 0, 1);
                    texName = tex[i].name + "_side_black.png";
                    break;
                //case BakePlan.MergeRGBA:    //�����
                    //backColor = new Color(0, 0, 0, 0);
                    //texName = tex[i].name + "_side_rgba.tga";
                    //break;
                case BakePlan.MergeRGB:    //�����
                    backColor = new Color(0, 0, 0, 0);
                    texName = tex[i].name + "_side_rgb.png";
                    break;
            }

            Debug.Log(string.Format("  ��ʼ�決��      ��ǰ��ʱ {0} ��", (Time.realtimeSinceStartup - ttt)));
            //texProgress = string.Format("  ��ʼ�決��      ��ǰ��ʱ {0} ��", (Time.realtimeSinceStartup - ttt));
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
                        case SideType.inSide:    //�����
                            if (color.a < alphaValue)
                            {
                                if (bakePlan == BakePlan.MergeRGB) newTex.SetPixel(w, h, tex[i].GetPixel(w, h));    // || bakePlan == BakePlan.MergeRGBA
                                else newTex.SetPixel(w, h, backColor);
                                continue;
                            }
                            break;
                        case SideType.outSide:    //�����
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

                    //��������ܱ�n�����أ���û�з���������
                    tempCheckList.Clear();
                    tempCheckList.Add(string.Format("{0},{1}", w - 1, h));
                    tempCheckList.Add(string.Format("{0},{1}", w + 1, h));
                    tempCheckList.Add(string.Format("{0},{1}", w, h - 1));
                    tempCheckList.Add(string.Format("{0},{1}", w, h + 1));
                    bool find = false;
                    int dis = 0;

                    bool side4 = false;
                    for (int k = 0; k < range; k++)    //����ÿ���Ӽ�Ҫ����
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
                            //value -1��Ч  0������   1����
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
                                break;    //�����һ���м�ֵ�� ��ǰ������Ǹ߼�ֵ����
                            }
                        }

                        if (!find) dis++;
                    }
                    if (dis == range)  //�ﵽ�߽��� ��û�з���������
                    {
                        if (!side4)
                        {
                            if (bakePlan == BakePlan.MergeRGB) newTex.SetPixel(w, h, tex[i].GetPixel(w, h));    // || bakePlan == BakePlan.MergeRGBA
                            else newTex.SetPixel(w, h, backColor);
                            continue;
                        }
                        
                    }
                    float v = 1;
                    if (attenuation)   //˥��
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
                    

                    if (sec > 1000)     //��ֵԽ�̣�������ˢ��Խ��  20000   -   50000
                    {
                        per += sec;
                        sec = 0;
                        Debug.Log(string.Format("           ����ɣ� {0} / {1}   (pixels)                �ٷֱȣ�  {2} %", per, max, (per / max * 100)));   // + "   ��ʱ " + ((Time.realtimeSinceStartup - ttt) / 60 )
                                                                                                                                                    //if (per > 512) yield return null;
                        yield return new WaitForSeconds(0.001f);
                    }
                }
            }
            newTex.Apply(false);

            yield return new WaitForSeconds(0.001f);
            Debug.Log(string.Format("    ͼƬ�決���, ���� {0} ��", ((Time.realtimeSinceStartup - ttt) / 60)));
            //yield return new WaitForSeconds(0.1f);
            string url = outPath + texName;
            WriteTexture(newTex, url);
            yield return new WaitForSeconds(0.001f);
            Debug.Log(string.Format("    �決ͼ�������, ������ {0} ��", ((Time.realtimeSinceStartup - ttt) / 60)));
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
        //File.WriteAllBytes(url);//����

        FileStream file = File.Open(url, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();

        UnityEditor.AssetDatabase.Refresh();
    }
}
