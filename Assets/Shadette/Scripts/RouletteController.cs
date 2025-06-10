// Scripts/RouletteController.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public class RouletteController : MonoBehaviour
{
    [System.Serializable]
    public enum RouletteColorType   // 色の配置方法
    {
        Random, // ランダムな色
        PieChart, // 円グラフ状の色
        GradientStripe, // グラデーションストライプ
        Stripe // ストライプ模様
    }

    [System.Serializable]
    public struct RouletteColorData
    {
        public RouletteColorType colorType; // 色の配置方法
        public Color fowardColor; // 360要素のColor
        public Color backwardColor; // 360要素のColor
        public float fowardRatio; // fowardColorの比率（0～1）
        //public Color[] colors; // ルーレットの色データ
        public int divisionCount; // 分割数
        public int baseSpeed; // 基本回転速度
        public int randomSpeed; // ランダム回転速度
        public RouletteColorData(RouletteColorType colorType,Color foward, Color backward, float fowardRatio, int division, int baseSpeed, int randomSpeed)
        {
            this.colorType = colorType;
            this.fowardColor = foward;
            this.backwardColor = backward;
            this.fowardRatio = fowardRatio;
            this.divisionCount = division;
            this.baseSpeed = baseSpeed;
            this.randomSpeed = randomSpeed;
        }
    }
    [Tooltip("ルーレットの色データセット（ScriptableObject）")]
    [SerializeField] private RouletteColorDataSet rouletteColorDataSet;

    private RouletteColorData[] RouletteColorDataArray => rouletteColorDataSet != null ? rouletteColorDataSet.colorDataArray : null;

    [Tooltip("ルーレット間のZ軸方向の距離")]
    [SerializeField] private float layerGap = 0.1f;

    private List<GameObject> _rouletteWheels = new List<GameObject>();
    private List<float> _rotationSpeeds = new List<float>();
    private List<Color[]> _wheelColorData = new List<Color[]>();
    private bool _isSpinning = true;

    void Start()
    {
        CreateRoulettes();
    }

    void Update()
    {
        if (_isSpinning)
        {
            for (int i = 0; i < _rouletteWheels.Count; i++)
            {
                _rouletteWheels[i].transform.Rotate(0, 0, _rotationSpeeds[i] * Time.deltaTime);
            }
        }
    }

    private void CreateRoulettes()
    {
        // 加算合成用のマテリアルを作成
        Material additiveMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        // シェーダーのTintColorを白（ニュートラル）に設定
        // これにより頂点カラーがそのままの色で表示される
        additiveMaterial.SetColor("_TintColor", Color.white);
        var numberOfWheels = RouletteColorDataArray?.Length ?? 0;
        if (numberOfWheels <= 0)
        {
            Debug.Log("No roulette data found.");
            return;
        }
        var colorDataArray = RouletteColorDataArray;
        if (colorDataArray == null)
        {
            Debug.Log("DataArray is not found");
            return;
        }
        for (int i = 0; i < numberOfWheels; i++)
        {
            Debug.Log(colorDataArray[i].fowardColor);
            // 1. ルーレットの色データを生成
            //Color[] colors = GetStripeTable(
            //    colorDataArray[i].fowardColor,
            //    colorDataArray[i].backwardColor,
            //    colorDataArray[i].fowardRatio,
            //    colorDataArray[i].divisionCount
            //); 
            Color[] colors;
            switch(colorDataArray[i].colorType)
            {
                case RouletteColorType.Random:
                    colors = GetRandomTable();
                    break;
                case RouletteColorType.PieChart:
                    colors = GetPieChartTable(
                        colorDataArray[i].fowardColor,
                        colorDataArray[i].backwardColor,
                        colorDataArray[i].fowardRatio
                    );
                    break;
                case RouletteColorType.GradientStripe:
                    colors = GetGradientStripeTable(
                        colorDataArray[i].fowardColor,
                        colorDataArray[i].backwardColor,
                        colorDataArray[i].divisionCount
                    );
                    break;
                case RouletteColorType.Stripe:
                default:
                    colors = GetStripeTable(
                        colorDataArray[i].fowardColor,
                        colorDataArray[i].backwardColor,
                        colorDataArray[i].fowardRatio,
                        colorDataArray[i].divisionCount
                    );
                    break;
            }


            // 2. ルーレットのGameObjectを生成
            GameObject wheel = new GameObject($"RouletteWheel_{i}");
            wheel.transform.SetParent(this.transform);
            wheel.transform.position = new Vector3(0, 0, i * layerGap);

            // 3. メッシュとレンダラーを追加
            MeshFilter meshFilter = wheel.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = wheel.AddComponent<MeshRenderer>();

            // 4. メッシュを生成して割り当て
            meshFilter.mesh = RouletteGenerator.CreateRouletteMesh(colors);
            meshRenderer.material = additiveMaterial;

            // 5. リストに追加
            _rouletteWheels.Add(wheel);
            _rotationSpeeds.Add(
                UnityEngine.Random.Range(
                colorDataArray[i].baseSpeed - colorDataArray[i].randomSpeed,
                colorDataArray[i].baseSpeed + colorDataArray[i].randomSpeed
                    ));
        }
    }

    // ランダムなルーレット色配列を生成するメソッド
    private Color[] GetRandomTable()
    {
        Color[] colors = new Color[360];
        for (int j = 0; j < 360; j++)
        {
            // ランダムな輝度のグレースケール色を生成
            // 当たり（高輝度）とハズレ（低輝度/黒）をランダムに配置
            float brightness = (UnityEngine.Random.Range(0, 5) == 0) ? UnityEngine.Random.Range(0.5f, 1.0f) : 0f;
            colors[j] = new Color(brightness, brightness, brightness, 1f);
        }
        return colors;
    }
    /// <summary>
    /// 2色と比率で円グラフ状に色配列を生成します。
    /// fowardRatioが0.75なら、360要素中270個がfowardColor、残りがbackwardColorになります。
    /// </summary>
    /// <param name="fowardColor">メインとなる色</param>
    /// <param name="backwardColor">サブとなる色</param>
    /// <param name="fowardRatio">メイン色の比率（0～1）</param>
    /// <returns>360要素のColor配列</returns>
    private Color[] GetPieChartTable(Color fowardColor, Color backwardColor, float fowardRatio)
    {
        Color[] colors = new Color[360];
        int fowardCount = Mathf.RoundToInt(360 * Mathf.Clamp01(fowardRatio));
        int backwardCount = 360 - fowardCount;

        for (int i = 0; i < fowardCount; i++)
        {
            colors[i] = fowardColor;
        }
        for (int i = fowardCount; i < 360; i++)
        {
            colors[i] = backwardColor;
        }
        return colors;
    }





    /// <summary>
    /// 2色をsinカーブでなだらかにグラデーションするストライプ模様のルーレット色配列を生成します。
    /// </summary>
    /// <param name="color1">ピークとなる色</param>
    /// <param name="color2">谷となる色</param>
    /// <param name="divisionCount">ピークの個数（sin波の山の数）</param>
    /// <returns>360要素のColor配列</returns>
    private Color[] GetGradientStripeTable(Color color1, Color color2, int divisionCount)
    {
        if (divisionCount <= 0)
        {
            Debug.LogError("分割数（divisionCount）は1以上である必要があります。");
            divisionCount = 1;
        }

        Color[] colors = new Color[360];
        float freq = divisionCount / 360f * 2f * Mathf.PI; // 1周でdivisionCount回ピーク

        for (int j = 0; j < 360; j++)
        {
            // sin波で0～1の値を生成（0がcolor2、1がcolor1）
            float t = (Mathf.Sin(j * freq) + 1f) / 2f;
            colors[j] = Color.Lerp(color2, color1, t);
        }

        return colors;
    }

    /// <summary>
    /// 2色を交互に配置したストライプ模様のルーレット色配列を生成します。
    /// </summary>
    /// <param name="color1">1色目の色</param>
    /// <param name="color2">2色目の色</param>
    /// <param name="divisionCount">円をいくつの縞模様に分割するか</param>
    /// <returns>360要素のColor配列</returns>
    private Color[] GetStripeTable(Color color1, Color color2, float ratio, int divisionCount)
    {
        if (divisionCount <= 0)
        {
            Debug.LogError("分割数（divisionCount）は1以上である必要があります。");
            divisionCount = 1;
        }

        Color[] colors = new Color[360];

        // 1つのセグメントのサイズ
        int segmentSize = 360 / divisionCount;
        if (segmentSize == 0) segmentSize = 1;

        int color1Length = Mathf.RoundToInt(segmentSize * ratio);
        int color2Length = segmentSize - color1Length;

        int idx = 0;
        for (int seg = 0; seg < divisionCount; seg++)
        {
            // color1部分
            for (int k = 0; k < color1Length && idx < 360; k++, idx++)
            {
                colors[idx] = color1;
            }
            // color2部分
            for (int k = 0; k < color2Length && idx < 360; k++, idx++)
            {
                colors[idx] = color2;
            }
        }
        // 余りがあれば最後にcolor2で埋める
        while (idx < 360)
        {
            colors[idx++] = color2;
        }

        return colors;
    }


    /// <summary>
    /// ルーレットの回転を停止する
    /// </summary>
    public void Stop()
    {
        _isSpinning = false;
    }


    /// <summary>
    /// ルーレットの回転を再開する
    /// </summary>
    public void Spin()
    {
        _isSpinning = true;
    }

    /// <summary>
    /// ルーレットが現在の回転角度で重なった状態で加算合成し、完全な白（R=1,G=1,B=1,A=1）になっている箇所の数をスコアとする
    /// </summary>
    /// <returns>白色の数（int）</returns>
    public int GetScore()
    {
        int whiteCount = 0;

        // 360分割の各角度ごとに
        for (int j = 0; j < 360; j++)
        {
            float r = 0f, g = 0f, b = 0f, a = 0f;

            // 各ルーレットの現在の回転角度を考慮して色を取得し加算
            for (int i = 0; i < _rouletteWheels.Count; i++)
            {
                // 現在のZ軸回転角度を取得
                float currentRotation = _rouletteWheels[i].transform.eulerAngles.z;
                // 実際に重なるインデックスを計算
                int colorIndex = Mathf.FloorToInt(Mathf.Repeat(j - currentRotation, 360));
                Color c = _wheelColorData[i][colorIndex];
                r += c.r;
                g += c.g;
                b += c.b;
                a += c.a;
            }

            // Clampで1.0を超えないようにする
            r = Mathf.Clamp01(r);
            g = Mathf.Clamp01(g);
            b = Mathf.Clamp01(b);
            a = Mathf.Clamp01(a);

            // 完全な白（R=1,G=1,B=1,A=1）と判定
            if (r >= 0.99f && g >= 0.99f && b >= 0.99f && a >= 0.99f)
            {
                whiteCount++;
            }
        }

        return whiteCount;
    }


    /// <summary>
    /// 12時の位置にあるセグメントのインデックスと輝度の合計値を取得する
    /// </summary>
    /// <returns>合計スコア</returns>
    public float GetScore0digree()
    {
        float totalScore = 0f;

        for (int i = 0; i < _rouletteWheels.Count; i++)
        {
            // 現在のZ軸回転角度を取得
            float currentRotation = _rouletteWheels[i].transform.eulerAngles.z;

            // Unityの角度系（上向きが90°）を考慮して、12時の位置にあるセグメントのインデックスを計算
            // (90 - rotation) で12時方向のセグメントを計算し、Repeatで0-360の範囲に収める
            int segmentIndex = Mathf.FloorToInt(Mathf.Repeat(90 - currentRotation, 360));

            // 該当インデックスの色の輝度（grayscale値）を加算
            totalScore += _wheelColorData[i][segmentIndex].grayscale;
        }

        // 仕様に合わせて点数を100点満点のように見せる（例として100倍）
        return totalScore * 100f;
    }
}