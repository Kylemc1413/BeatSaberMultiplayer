﻿using BeatSaberMultiplayer.UI.ViewControllers;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRUI;

namespace BeatSaberMultiplayer.UI.FlowCoordinators
{
    class ModeSelectionFlowCoordinator : FlowCoordinator
    {
        MultiplayerNavigationController _multiplayerNavController;
        ModeSelectionViewController _selectionViewController;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                title = "Select Mode";

                _multiplayerNavController = BeatSaberUI.CreateViewController<MultiplayerNavigationController>();
                _multiplayerNavController.didFinishEvent += () => {
                    MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                    mainFlow.InvokeMethod("DismissFlowCoordinator", this, null, false);
                };
                
                _selectionViewController = BeatSaberUI.CreateViewController<ModeSelectionViewController>();
                _selectionViewController.didSelectRooms += () => { PresentFlowCoordinator(PluginUI.instance.serverHubFlowCoordinator);  };
                _selectionViewController.didSelectRadio += () => { PresentFlowCoordinator(PluginUI.instance.channelSelectionFlowCoordinator); };

            }
            
            SetViewControllerToNavigationConctroller(_multiplayerNavController, _selectionViewController);
            ProvideInitialViewControllers(_multiplayerNavController, null, null);
        }
    }
}
