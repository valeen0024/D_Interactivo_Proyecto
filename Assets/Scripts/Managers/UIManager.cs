using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private Slider fuelBar;
    [SerializeField] private TMP_Text j1DistanceText;
    [SerializeField] private TMP_Text j2DistanceText;

    [Header("Gameplay References")]
    [SerializeField] private KartFuelSystem playerFuelSystem;
    [SerializeField] private KartFuelSystem cpuFuelSystem;

    [SerializeField] private DistanceTracker playerDistanceTracker;
    [SerializeField] private DistanceTracker cpuDistanceTracker;

    [Header("Winner UI")]
    [SerializeField] private GameObject uiPlay;
    [SerializeField] private GameObject uiWin;
    [SerializeField] private GameObject btnMainMenu;

    [SerializeField] private GameObject imgGanaTitle; // Arrastra aquí "TX WIN" (la imagen de "GANA!")
    [SerializeField] private TMP_Text txtWinJ1; 
    [SerializeField] private TMP_Text txtWinJ2; 
    [SerializeField] private GameObject imgTie; 

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip tieSound;

    private bool winnerShown = false;

    private void Update()
    {
        UpdateFuelBar();
        UpdateDistance();

        if (!winnerShown &&
            GameManager.Instance != null &&
            GameManager.Instance.gameState == GameManager.GameState.Finished)
        {
            ShowWinnerUI();
        }
    }

    private void UpdateFuelBar()
    {
        if (fuelBar == null) return;

        if (GameManager.Instance.gameState == GameManager.GameState.PlayerTurn)
        {
            if (playerFuelSystem != null) fuelBar.value = playerFuelSystem.GetFuelNormalized();
        }
        else if (GameManager.Instance.gameState == GameManager.GameState.CPUTurn)
        {
            if (cpuFuelSystem != null) fuelBar.value = cpuFuelSystem.GetFuelNormalized();
        }
    }

    private void UpdateDistance()
    {
        if (playerDistanceTracker != null && j1DistanceText != null)
        {
            j1DistanceText.text = GameData.PlayerName + "\n" + playerDistanceTracker.currentDistance.ToString("F1") + " YDS";
        }

        if (cpuDistanceTracker != null && j2DistanceText != null)
        {
            j2DistanceText.text = "CONTRINCANTE\n" + cpuDistanceTracker.currentDistance.ToString("F1") + " YDS";
        }
    }

    public void ShowWinnerUI()
    {
        winnerShown = true;

        uiPlay.SetActive(false);
        uiWin.SetActive(true);

        if (btnMainMenu != null && GameManager.Instance != null)
        {
            btnMainMenu.SetActive(!GameManager.Instance.AutoAdvanceToNextLevel);
        }

        // Apagamos todo al inicio para encender solo lo que corresponde
        if (imgGanaTitle != null) imgGanaTitle.SetActive(false);
        if (txtWinJ1 != null) txtWinJ1.gameObject.SetActive(false);
        if (txtWinJ2 != null) txtWinJ2.gameObject.SetActive(false);
        if (imgTie != null) imgTie.SetActive(false);

        switch (GameManager.Instance.winnerName)
        {
            case "Player":
                if (imgGanaTitle != null) imgGanaTitle.SetActive(true);
                if (txtWinJ1 != null)
                {
                    txtWinJ1.text = GameData.PlayerName; 
                    txtWinJ1.gameObject.SetActive(true);
                }
                PlaySound(winSound);
                break;

            case "CPU":
                if (imgGanaTitle != null) imgGanaTitle.SetActive(true);
                if (txtWinJ2 != null)
                {
                    txtWinJ2.text = "CONTRINCANTE";
                    txtWinJ2.gameObject.SetActive(true);
                }
                PlaySound(loseSound);
                break;

            case "Draw":
                // Oculta "GANA!" y los nombres, y muestra únicamente el objeto de empate
                if (imgTie != null) imgTie.SetActive(true);
                PlaySound(tieSound);
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}