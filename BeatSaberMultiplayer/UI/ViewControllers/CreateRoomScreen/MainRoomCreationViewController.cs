﻿using BeatSaberMultiplayer.Data;
using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI.UIElements;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaberMultiplayer.UI.ViewControllers.CreateRoomScreen
{
    class MainRoomCreationViewController : VRUIViewController
    {
        public event Action didFinishEvent;
        public event Action<CustomKeyboardViewController> keyboardDidFinishEvent;
        public event Action<RoomSettings> CreatedRoom;

        private Button _backButton;
        private Button _editNameButton;
        private Button _editPasswordButton;
        private OnOffViewController _usePasswordToggle;
        private CustomListViewController _songSelectionList;
        private CustomListViewController _maxPlayersList;
        private OnOffViewController _noFailToggle;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _passwordText;

        private Button _createRoomButton;

        private CustomKeyboardViewController _nameKeyboard;
        private CustomKeyboardViewController _passwordKeyboard;

        private string _roomName;
        private string _roomPassword;
        private SongSelectionType _songSelectionType;
        private int _maxPlayers = 0;
        private bool _usePassword = false;
        private bool _noFailMode = true;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if(firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(delegate () { didFinishEvent?.Invoke(); });

                _nameKeyboard = BeatSaberUI.CreateViewController<CustomKeyboardViewController>();
                _nameKeyboard.enterButtonPressed += NameEntered;
                _nameKeyboard.backButtonPressed += () => { keyboardDidFinishEvent?.Invoke(_nameKeyboard); };
                _passwordKeyboard = BeatSaberUI.CreateViewController<CustomKeyboardViewController>();
                _passwordKeyboard.enterButtonPressed += PasswordEntered;
                _passwordKeyboard.backButtonPressed += () => { keyboardDidFinishEvent?.Invoke(_passwordKeyboard);};

                _usePasswordToggle = CustomSettingsHelper.AddToggleSetting<OnOffViewController>(rectTransform, "Use Password");
                (_usePasswordToggle.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_usePasswordToggle.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_usePasswordToggle.transform as RectTransform).anchoredPosition = new Vector2(0f, 10f);
                _usePasswordToggle.ValueChanged += UsePasswordToggle_ValueChanged;
                _usePasswordToggle.Value = _usePassword;

                _songSelectionList = CustomSettingsHelper.AddListSetting<CustomListViewController>(rectTransform, "Song Selection");
                (_songSelectionList.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_songSelectionList.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_songSelectionList.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                _songSelectionList.ValueChanged += SongSelection_ValueChanged;
                _songSelectionList.maxValue = 2;
                _songSelectionList.Value = (int)_songSelectionType;
                _songSelectionList.textForValues = new string[] { "Manual", "Random", "Voting" };
                _songSelectionList.UpdateText();

                _maxPlayersList = CustomSettingsHelper.AddListSetting<CustomListViewController>(rectTransform, "Max Players");
                (_maxPlayersList.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_maxPlayersList.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_maxPlayersList.transform as RectTransform).anchoredPosition = new Vector2(0f, -10f);
                _maxPlayersList.ValueChanged += MaxPlayers_ValueChanged;
                _maxPlayersList.Value = _maxPlayers;
                _maxPlayersList.maxValue = 16;

                _noFailToggle = CustomSettingsHelper.AddToggleSetting<OnOffViewController>(rectTransform, "No Fail Mode");
                (_noFailToggle.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_noFailToggle.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_noFailToggle.transform as RectTransform).anchoredPosition = new Vector2(0f, -20f);
                _noFailToggle.ValueChanged += NoFailToggle_ValueChanged;
                _noFailToggle.Value = _noFailMode;

                _roomName = $"{GetUserInfo.GetUserName()}'s room".ToUpper();
                _nameText = BeatSaberUI.CreateText(rectTransform, _roomName, new Vector2(-22.5f, 25f));
                _nameText.fontSize = 5f;

                _editNameButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _editNameButton.SetButtonText("EDIT NAME");
                (_editNameButton.transform as RectTransform).sizeDelta = new Vector2(34f, 8f);
                (_editNameButton.transform as RectTransform).anchoredPosition = new Vector2(34f, 25f);
                _editNameButton.onClick.RemoveAllListeners();
                _editNameButton.onClick.AddListener(delegate ()
                {
                    _nameKeyboard._inputString = _roomName;
                    PluginUI.instance.roomCreationFlowCoordinator.PresentKeyboard(_nameKeyboard);
                });
                
                _roomPassword = "";
                _passwordText = BeatSaberUI.CreateText(rectTransform, "ENTER PASSWORD", new Vector2(-22.5f, 15f));
                _passwordText.fontSize = 5f;

                _editPasswordButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _editPasswordButton.SetButtonText("EDIT PASS");
                (_editPasswordButton.transform as RectTransform).sizeDelta = new Vector2(34f, 8f);
                (_editPasswordButton.transform as RectTransform).anchoredPosition = new Vector2(34f, 15f);
                _editPasswordButton.onClick.RemoveAllListeners();
                _editPasswordButton.onClick.AddListener(delegate ()
                {
                    _passwordKeyboard._inputString = _roomPassword;
                    PluginUI.instance.roomCreationFlowCoordinator.PresentKeyboard(_passwordKeyboard);
                });

                _createRoomButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _createRoomButton.SetButtonText("Create Room");
                (_createRoomButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_createRoomButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -32.5f);
                _createRoomButton.onClick.RemoveAllListeners();
                _createRoomButton.onClick.AddListener(delegate () {
                    CreatedRoom?.Invoke(new RoomSettings() { Name = _roomName, UsePassword = _usePassword, Password = _roomPassword, NoFail = _noFailMode, MaxPlayers = _maxPlayers, SelectionType = _songSelectionType});
                });
                _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
            }
            else
            {
                _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
            }

        }

        private void SongSelection_ValueChanged(int obj)
        {
            _songSelectionType = (SongSelectionType)obj;
        }

        private void MaxPlayers_ValueChanged(int obj)
        {
            _maxPlayers = obj;
        }

        private void PasswordEntered(string obj)
        {
            keyboardDidFinishEvent?.Invoke(_passwordKeyboard);
            _roomPassword = obj.ToUpper();
            _passwordText.text = string.IsNullOrEmpty(obj) ? "ENTER PASSWORD" : obj.ToUpper();
            _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
        }

        private void NameEntered(string obj)
        {
            keyboardDidFinishEvent?.Invoke(_nameKeyboard);
            _roomName = obj.ToUpper();
            _nameText.text = string.IsNullOrEmpty(obj) ? "ENTER ROOM NAME" : obj.ToUpper();
            _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
        }

        private void NoFailToggle_ValueChanged(bool value)
        {
            _noFailMode = value;
        }

        private void UsePasswordToggle_ValueChanged(bool value)
        {
            _usePassword = value;
            _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
        }

        public void Update()
        {
            if(isInViewControllerHierarchy)
                _createRoomButton.interactable = PluginUI.instance.roomCreationFlowCoordinator.CheckRequirements();
        }

        public void CreateButtonInteractable(bool interactable)
        {
            _createRoomButton.interactable = interactable;
        }

        public bool CheckRequirements()
        {
            return (!_usePassword || (_usePassword && !string.IsNullOrEmpty(_roomPassword))) && !string.IsNullOrEmpty(_roomName);
        }
        
    }
}
