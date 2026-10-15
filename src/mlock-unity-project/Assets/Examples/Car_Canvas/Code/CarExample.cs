using System.Threading.Tasks;
using Migs.Examples.Shared;
using Migs.MLock.Examples.Shared;
using Migs.MLock.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Migs.MLock.Examples.Car_Canvas.Code
{
    /// <summary>
    /// Example demonstrating MLock with a car and menu UI using Unity Canvas
    /// GetComponentsInChildren() is used to simplify the example, but please, do not use it in production code
    /// </summary>
    public class CarExample : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _tempLockButton;
        [SerializeField] private GameObject _menuPopup;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _selectLanguageButton;
        [SerializeField] private GameObject _languageTooltip;
        [SerializeField] private Button[] _languageButtons;

        #endregion

        #region Locks

        private LockHandle<CarLockTags> _gameLock;
        private LockHandle<CarLockTags> _menuLock;

        #endregion

        #region Initialization

        private void Start()
        {
            InitializeUI();
            SetupUIEventHandlers();
        }

        private void InitializeUI()
        {
            _menuPopup.SetActive(false);
            _languageTooltip.SetActive(false);
        }

        private void SetupUIEventHandlers()
        {
            _menuButton.onClick.AddListener(OnMenuButtonClicked);
            _tempLockButton.onClick.AddListener(OnTempLockButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
            _selectLanguageButton.onClick.AddListener(OnSelectLanguageButtonClicked);

            foreach (var button in _languageButtons)
            {
                button.onClick.AddListener(() => OnLanguageSelected(button.GetComponentInChildren<TMP_Text>().text));
            }
        }

        #endregion

        #region Cleanup

        private void OnDisable()
        {
            CleanupUIEventHandlers();
        }

        private void CleanupUIEventHandlers()
        {
            _menuButton.onClick.RemoveListener(OnMenuButtonClicked);
            _tempLockButton.onClick.RemoveListener(OnTempLockButtonClicked);
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
            _selectLanguageButton.onClick.RemoveListener(OnSelectLanguageButtonClicked);

            foreach (var button in _languageButtons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region Button Callbacks - Magic happens here

        // This method demonstrates how to use the `using` statement with MLock
        private async void OnTempLockButtonClicked()
        {
            var textComponent = _tempLockButton.GetComponentInChildren<TMP_Text>();
            var originalText = textComponent.text;

            async Task LockCountdown(int duration)
            {
                for (var i = 0; i < duration; i++)
                {
                    textComponent.text = $"Locked ({duration - i})";
                    await Task.Delay(1000);
                }

                textComponent.text = originalText;
            }

            Debug.Log("Locking everything for the duration of the `using` block");

            using (CarLockService.Instance.LockAll())
            {
                await LockCountdown(3);
            } // The lock will be released after this line

            Debug.Log("Lock released");


            Debug.Log("Locking everything for the rest of the method");
            using var @lock = CarLockService.Instance.LockAll();

            await LockCountdown(3);

            Debug.Log("Finished counting down from 3, but the lock isn't lifted yet");
            Debug.Log("Counting down again from 3");

            await LockCountdown(3);
            Debug.Log("After this log the lock will be lifted");
        }

        private void OnMenuButtonClicked()
        {
            _menuPopup.SetActive(true);

            // Lock everything beneath the menu when it is opened
            _gameLock = CarLockService.Instance.Lock(CarLockTags.PlayerInput | CarLockTags.GameHud);
        }

        private void OnExitButtonClicked()
        {
            _menuPopup.SetActive(false);
            _languageTooltip.SetActive(false);

            _gameLock.Dispose();
        }

        private void OnSelectLanguageButtonClicked()
        {
            _languageTooltip.SetActive(true);

            // Lock the main menu when language tooltip is opened
            _menuLock = CarLockService.Instance.Lock(CarLockTags.MainMenu);
        }

        private void OnLanguageSelected(string language)
        {
            Debug.Log($"Selected language: {language}");

            // Close language tooltip
            _languageTooltip.SetActive(false);

            _menuLock.Dispose();
        }

        #endregion

    }
}