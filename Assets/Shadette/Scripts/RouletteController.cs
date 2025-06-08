// Scripts/RouletteController.cs

using System.Collections.Generic;
using UnityEngine;

public class RouletteController : MonoBehaviour
{
    [Tooltip("重ねるルーレットの数")]
    [SerializeField] private int numberOfWheels = 5;

    [Tooltip("ルーレットの回転速度（度/秒）の最小値")]
    [SerializeField] private float minSpeed = 50f;

    [Tooltip("ルーレットの回転速度（度/秒）の最大値")]
    [SerializeField] private float maxSpeed = 200f;

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

        for (int i = 0; i < numberOfWheels; i++)
        {
            // 1. ルーレットの色データを生成
            Color[] colors = GetStripeTable(
                new Color(0.5f, 0.5f, 0.5f, 1f), // 当たり色
                new Color(0f, 0f, 0f, 1f),   // ハズレ色（黒）
                i+2           // 分割数
            );
            _wheelColorData.Add(colors);

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
            _rotationSpeeds.Add(Random.Range(minSpeed, maxSpeed));
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
            float brightness = (Random.Range(0, 5) == 0) ? Random.Range(0.5f, 1.0f) : 0f;
            colors[j] = new Color(brightness, brightness, brightness, 1f);
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
    private Color[] GetStripeTable(Color color1, Color color2, int divisionCount)
    {
        // divisionCountが0以下の場合、エラーを防ぐ
        if (divisionCount <= 0)
        {
            Debug.LogError("分割数（divisionCount）は1以上である必要があります。");
            // エラーケースとして、color1単色の配列を返す
            divisionCount = 1; 
        }

        Color[] colors = new Color[360];
        
        // 1つの色の帯が何°分（配列の要素数）になるかを計算
        // 例: divisionCountが12なら、360/12 = 30要素ごと
        int segmentSize = 360 / divisionCount;
        if (segmentSize == 0) segmentSize = 1; // divisionCountが360を超える場合の対策

        for (int j = 0; j < 360; j++)
        {
            // 現在の角度(j)が、何番目のセグメント（色の帯）に属するかを計算
            int segmentIndex = j / segmentSize;

            // セグメントの番号が偶数か奇数かによって色を決定する
            if (segmentIndex % 2 == 0)
            {
                // 偶数番目の帯は color1
                colors[j] = color1;
            }
            else
            {
                // 奇数番目の帯は color2
                colors[j] = color2;
            }
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
    /// 12時の位置にあるセグメントのインデックスと輝度の合計値を取得する
    /// </summary>
    /// <returns>合計スコア</returns>
    public float GetScore()
    {
        float totalScore = 0f;
        
        for(int i = 0; i < _rouletteWheels.Count; i++)
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