using System.Threading.Tasks;
using Migs.Examples.Shared;
using Migs.MLock.Examples.Shared;
using Migs.MLock.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace Migs.MLock.Examples.Car_UiElements.Code
{
    /// <summary>
    /// Example demonstrating MLock with a car and menu UI
    /// </summary>
    public class CarExample : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private CarExampleUiBuilder _uiBuilder;

        #endregion

        #region Controls

        private Button _menuButton;
        private Button _tempLockButton;
        private VisualElement _menuPopup;
        private Button _exitButton;
        private Button _selectLanguageButton;
        private VisualElement _languageTooltip;
        private Button[] _languageButtons;

        #endregion

        #region Locks
        
        private LockHandle<CarLockTags> _gameLock;
        private LockHandle<CarLockTags> _menuLock;

        #endregion

        #region Initialization
        
        private void OnEnable()
        {
            _uiBuilder.BuildUI();
            InitializeUI();
            SetupUIEventHandlers();
        }
        
        private void InitializeUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            // Get references to UI elements
            _menuButton = root.Q<Button>(CarExampleUiBuilder.MenuButtonName);
            _tempLockButton = root.Q<Button>(CarExampleUiBuilder.TempLockButtonName);
            _menuPopup = root.Q<VisualElement>(CarExampleUiBuilder.MenuPopupName);
            _exitButton = root.Q<Button>(CarExampleUiBuilder.ExitButtonName);
            _selectLanguageButton = root.Q<Button>(CarExampleUiBuilder.SelectLanguageButtonName);
            _languageTooltip = root.Q<VisualElement>(CarExampleUiBuilder.LanguageTooltipName);
            
            // Get language buttons
            _languageButtons = new[]
            {
                _languageTooltip.Q<Button>(CarExampleUiBuilder.EnglishLanguageButtonName),
                _languageTooltip.Q<Button>(CarExampleUiBuilder.DeutschLanguageButtonName),
                _languageTooltip.Q<Button>(CarExampleUiBuilder.FrenchLanguageButtonName)
            };
            
            // Initialize UI state
            _menuPopup.style.display = DisplayStyle.None;
            _languageTooltip.style.display = DisplayStyle.None;
        }
        
        private void SetupUIEventHandlers()
        {
            _menuButton.clicked += OnMenuButtonClicked;
            _tempLockButton.clicked += OnTempLockButtonClicked;
            _exitButton.clicked += OnExitButtonClicked;
            _selectLanguageButton.clicked += OnSelectLanguageButtonClicked;
            
            foreach (var button in _languageButtons)
            {
                button.clickable.clickedWithEventInfo += OnLanguageButtonClicked;
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
            _menuButton.clicked -= OnMenuButtonClicked;
            _tempLockButton.clicked -= OnTempLockButtonClicked;
            _exitButton.clicked -= OnExitButtonClicked;
            _selectLanguageButton.clicked -= OnSelectLanguageButtonClicked;
            
            foreach (var button in _languageButtons)
            {
                button.clickable.clickedWithEventInfo -= OnLanguageButtonClicked;
            }
        }

        #endregion

        #region Button Callbacks - Magic happens here

        // This method demonstrates how to use the `using` statement with MLock
        private async void OnTempLockButtonClicked()
        {
            var originalText = _tempLockButton.text;
            
            async Task LockCountdown(int duration)
            {
                for (var i = 0; i < duration; i++)
                {
                    _tempLockButton.text = $"Locked ({duration - i})";
                    await Task.Delay(1000);
                }

                _tempLockButton.text = originalText;
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

        private void OnLanguageButtonClicked(EventBase obj)
        {
            OnLanguageSelected(((Button)obj.target).text);
        }
        
        private void OnMenuButtonClicked()
        {
            _menuPopup.style.display = DisplayStyle.Flex;
            
            // Lock everything beneath the menu when it is opened
            _gameLock = CarLockService.Instance.Lock(CarLockTags.PlayerInput | CarLockTags.GameHud);
        }
        
        private void OnExitButtonClicked()
        {
            _menuPopup.style.display = DisplayStyle.None;
            _languageTooltip.style.display = DisplayStyle.None;
            
            _gameLock.Dispose();
        }
        
        private void OnSelectLanguageButtonClicked()
        {
            _languageTooltip.style.display = DisplayStyle.Flex;
            
            // Lock the main menu when language tooltip is opened
            _menuLock = CarLockService.Instance.Lock(CarLockTags.MainMenu);
        }
        
        private void OnLanguageSelected(string language)
        {
            Debug.Log($"Selected language: {language}");
            
            // Close language tooltip
            _languageTooltip.style.display = DisplayStyle.None;
            
            _menuLock.Dispose();
        }

        #endregion
        
    }
} 