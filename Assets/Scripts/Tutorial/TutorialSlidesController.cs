using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialSlidesController : MonoBehaviour
{
    [Header("Diapositivas del Tutorial (Opcional si los sprites van en cada Grupo)")]
    [SerializeField] private Image slideDisplayImage;
    [SerializeField] private Sprite[] tutorialSlides;

    [Header("Grupos de Contenido por Página (3 Objetos)")]
    [SerializeField] private GameObject[] pageTextGroups;

    [Header("Botones sobre las imágenes")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button mainActionButton;

    [Header("Conexión con la Práctica")]
    [SerializeField] private GameObject visualTutorialPanel;
    [SerializeField] private GameObject hudPlayToRestore;
    [SerializeField] private GameObject practicalTutorialUI; // <-- Nuevo: UI Practical Tutorial

    private int currentPage = 0;

    private void Start()
    {
        Time.timeScale = 0f;

        // Ocultar tanto el HUD de juego como el letrero de entrenamiento mientras se leen las 3 páginas
        if (hudPlayToRestore != null)
            hudPlayToRestore.SetActive(false);

        if (practicalTutorialUI != null)
            practicalTutorialUI.SetActive(false);

        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (mainActionButton != null) mainActionButton.onClick.AddListener(HandleMainButton);

        UpdateSlideUI();
    }

    private void Update()
    {
        if (visualTutorialPanel == null || !visualTutorialPanel.activeInHierarchy) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            HandleMainButton();
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            PreviousPage();
        }
    }

    public void NextPage()
    {
        if (currentPage < pageTextGroups.Length - 1)
        {
            currentPage++;
            UpdateSlideUI();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateSlideUI();
        }
    }

    private void HandleMainButton()
    {
        if (currentPage < pageTextGroups.Length - 1)
        {
            NextPage();
        }
        else
        {
            StartPracticeMode();
        }
    }

    private void UpdateSlideUI()
    {
        if (tutorialSlides != null && tutorialSlides.Length > currentPage && tutorialSlides[currentPage] != null && slideDisplayImage != null)
        {
            slideDisplayImage.sprite = tutorialSlides[currentPage];
        }

        if (pageTextGroups != null)
        {
            for (int i = 0; i < pageTextGroups.Length; i++)
            {
                if (pageTextGroups[i] != null)
                {
                    pageTextGroups[i].SetActive(i == currentPage);
                }
            }
        }

        if (prevButton != null)
            prevButton.gameObject.SetActive(currentPage > 0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(currentPage < pageTextGroups.Length - 1);
    }

    public void StartPracticeMode()
    {
        Time.timeScale = 1f;

        if (hudPlayToRestore != null)
            hudPlayToRestore.SetActive(true);

        // Activar el letrero de ENTRENAMIENTO PASO 1 DE 5
        if (practicalTutorialUI != null)
            practicalTutorialUI.SetActive(true);

        if (visualTutorialPanel != null)
            visualTutorialPanel.SetActive(false);
    }
}