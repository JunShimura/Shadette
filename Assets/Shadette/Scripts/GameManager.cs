// Scripts/GameManager.cs

using UnityEngine;
using TMPro; // または TMProの場合は using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private RouletteController rouletteController;
    [SerializeField] private TextMeshProUGUI scoreText; // TextMeshProを使用する場合は 
    [SerializeField] private GameObject arrowIndicator; // 矢印のUIオブジェクト

    private enum GameState { Spinning, Stopped, ScoreView }
    private GameState _currentState;

    void Start()
    {
        // 初期状態設定
        _currentState = GameState.Spinning;
        scoreText.gameObject.SetActive(false);
        arrowIndicator.SetActive(true);
    }

    void Update()
    {
        // マウスクリックを検出
        if (Input.GetMouseButtonDown(0))
        {
            switch (_currentState)
            {
                // 回転中にクリックされたら、ルーレットを止める
                case GameState.Spinning:
                    rouletteController.Stop();
                    DisplayScore();
                    _currentState = GameState.ScoreView;
                    break;
                
                // スコア表示中にクリックされたら、最初からやり直す
                case GameState.ScoreView:
                    RestartGame();
                    _currentState = GameState.Spinning;
                    break;
            }
        }
    }

    private void DisplayScore()
    {
        // スコアを計算して表示
        float score = rouletteController.GetScore();
        scoreText.text = $"SCORE：{score:F0}"; // F0は小数点以下なしの意味
        scoreText.gameObject.SetActive(true);
        arrowIndicator.SetActive(false); // 矢印を隠す
    }

    private void RestartGame()
    {
        // UIを初期状態に戻す
        scoreText.gameObject.SetActive(false);
        arrowIndicator.SetActive(true);

        // ルーレットを再度回転させる
        rouletteController.Spin();
    }
}