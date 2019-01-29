﻿using BeatSaberMultiplayer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using BeatSaberMultiplayer.Misc;
using SongLoaderPlugin;
using CustomUI.BeatSaber;
using System.Globalization;
using UnityEngine.Networking;
using SimpleJSON;
using System.Collections;

namespace BeatSaberMultiplayer.UI.ViewControllers.RadioScreen
{
    class ResultsScreenViewController : VRUIViewController
    {
        TextMeshProUGUI _timerText;

        TextMeshProUGUI _songNameText;
        TextMeshProUGUI _scoreText;
        TextMeshProUGUI _difficultyText;
        TextMeshProUGUI _rankText;
        TextMeshProUGUI _goodCutsText;
        TextMeshProUGUI _fullComboText;

        Button _favButton;
        Button _upvoteButton;
        Button _downvoteButton;

        TextMeshProUGUI _ratingText;

        private bool _firstVote;

        IDifficultyBeatmap lastSong;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                _timerText = BeatSaberUI.CreateText(rectTransform, "0:30", new Vector2(0f, 35f));
                _timerText.alignment = TextAlignmentOptions.Top;
                _timerText.fontSize = 8f;

                RectTransform resultsRectTransform = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "StandardLevelResultsViewController");
                Instantiate(resultsRectTransform.GetComponentsInChildren<RectTransform>().First(x => x.name == "Cleared"), rectTransform);

                _songNameText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "SongNameText");
                _scoreText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "ScoreText");
                _difficultyText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "DifficultyText");
                _rankText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "RankText");
                _goodCutsText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "GoodCutsText");
                _fullComboText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "FullComboText");
                Destroy(GetComponentsInChildren<RectTransform>().First(x => x.name == "HeaderPanel").gameObject);
                Destroy(GetComponentsInChildren<RectTransform>().First(x => x.name == "NewHighScoreText").gameObject);

                if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "BeatSaver Downloader"))
                {
                    _favButton = this.CreateUIButton("PracticeButton", new Vector2(65f, 25f), null, "", Sprites.addToFavorites);

                    _upvoteButton = this.CreateUIButton("PracticeButton", new Vector2(65f, 10f), null, "", Sprites.thumbUp);
                    _downvoteButton = this.CreateUIButton("PracticeButton", new Vector2(65f, -10f), null, "", Sprites.thumbDown);
                    _ratingText = this.CreateText("LOADING...", new Vector2(65f, 0f));
                    _ratingText.alignment = TextAlignmentOptions.Center;
                    _ratingText.fontSize = 7f;
                }
            }
        }

        public void SetSongInfo(SongInfo songInfo, BeatmapDifficulty difficulty)
        {
            lastSong = null;

            _songNameText.text = songInfo.songName;
            _scoreText.text = ScoreFormatter.Format(0);
            _difficultyText.text = difficulty.Name();
            _rankText.text = "E";
            _goodCutsText.text = "0<size=50%> / 0";
            _fullComboText.gameObject.SetActive(false);

            if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "BeatSaver Downloader"))
            {
                _favButton.interactable = false;
                _upvoteButton.interactable = false;
                _downvoteButton.interactable = false;
                _ratingText.text = "";
            }
        }

        public void SetSongInfo(IDifficultyBeatmap songInfo, LevelCompletionResults results)
        {
            if (lastSong != songInfo)
            {
                Misc.Logger.Info("Updating song info on results screen!");
                lastSong = songInfo;

                _songNameText.text = $"{songInfo.level.songName} <size=80%>{songInfo.level.songSubName}</size>";
                _scoreText.text = ScoreFormatter.Format(results.score);
                _difficultyText.text = songInfo.difficulty.Name();
                _rankText.text = RankModel.GetRankName(results.rank);
                _goodCutsText.text = $"{results.goodCutsCount}<size=50%> / {songInfo.beatmapData.notesCount}";
                _fullComboText.gameObject.SetActive(results.fullCombo);

                if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "BeatSaver Downloader"))
                {
                    _firstVote = true;

                    _favButton.onClick.RemoveAllListeners();
                    _favButton.onClick.AddListener(() => ToggleFavorite(songInfo.level.levelID));

                    BeatSaverDownloaderHelper.LoadDownloaderConfig();
                    _favButton.interactable = true;
                    _favButton.SetButtonIcon(BeatSaverDownloaderHelper.favoriteSongs.Any(x => x == songInfo.level.levelID) ? Sprites.removeFromFavorites : Sprites.addToFavorites);

                    VoteType vote = BeatSaverDownloaderHelper.GetVoteForSong(songInfo.level.levelID);

                    _upvoteButton.interactable = false;
                    _downvoteButton.interactable = false;
                    _ratingText.text = "LOADING...";

                    SongDownloader.Instance.RequestSongByLevelID(songInfo.level.levelID.Substring(0, 32), (song) =>
                    {
                        _ratingText.text = (int.Parse(song.upvotes) - int.Parse(song.downvotes)).ToString();

                        _upvoteButton.interactable = (vote != VoteType.Upvote && BeatSaverDownloaderHelper.apiAccessToken != BeatSaverDownloaderHelper.apiTokenPlaceholder);
                        _downvoteButton.interactable = (vote != VoteType.Downvote && BeatSaverDownloaderHelper.apiAccessToken != BeatSaverDownloaderHelper.apiTokenPlaceholder);

                        _upvoteButton.onClick.RemoveAllListeners();
                        _downvoteButton.onClick.RemoveAllListeners();
                        _upvoteButton.onClick.AddListener(() => StartCoroutine(VoteForSong(song.id, songInfo.level.levelID, true)));
                        _downvoteButton.onClick.AddListener(() => StartCoroutine(VoteForSong(song.id, songInfo.level.levelID, false)));
                    });
                }
            }
        }

        public void ToggleFavorite(string levelId)
        {
            if(BeatSaverDownloaderHelper.favoriteSongs.Any(x => x == levelId))
            {
                BeatSaverDownloaderHelper.favoriteSongs.Remove(levelId);
                _favButton.SetButtonIcon(Sprites.addToFavorites);
            }
            else
            {
                BeatSaverDownloaderHelper.favoriteSongs.Add(levelId);
                _favButton.SetButtonIcon(Sprites.removeFromFavorites);
            }
            BeatSaverDownloaderHelper.SaveDownloaderConfig();
        }

        public IEnumerator VoteForSong(string key, string levelId, bool upvote)
        {
            Misc.Logger.Info($"Voting for song... key: {key}, levelId: {levelId}, voteType: {(upvote ? "Upvote" : "Downvote")}");

            _upvoteButton.interactable = false;
            _downvoteButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"{Config.Instance.BeatSaverURL}/api/songs/vote/{key}/{(upvote ? 1 : 0)}/{BeatSaverDownloaderHelper.apiAccessToken}");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError || voteWWW.isHttpError)
            {
                Misc.Logger.Error(voteWWW.error);
                _ratingText.text = voteWWW.error;
            }
            else
            {
                if (!_firstVote)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }

                _firstVote = false;

                switch (voteWWW.responseCode)
                {
                    case 200:
                        {
                            JSONNode node = JSON.Parse(voteWWW.downloadHandler.text);
                            _ratingText.text = (int.Parse(node["upVotes"]) - int.Parse(node["downVotes"])).ToString();

                            if (upvote)
                            {
                                _upvoteButton.interactable = false;
                                _downvoteButton.interactable = true;
                            }
                            else
                            {
                                _downvoteButton.interactable = false;
                                _upvoteButton.interactable = true;
                            }

                            if (!BeatSaverDownloaderHelper.votedSongs.ContainsKey(levelId.Substring(0, 32)))
                            {
                                BeatSaverDownloaderHelper.votedSongs.Add(levelId.Substring(0, 32), new SongVote(key, upvote ? VoteType.Upvote : VoteType.Downvote));
                                BeatSaverDownloaderHelper.SaveDownloaderConfig();
                            }
                            else if (BeatSaverDownloaderHelper.votedSongs[levelId.Substring(0, 32)].voteType != (upvote ? VoteType.Upvote : VoteType.Downvote))
                            {
                                BeatSaverDownloaderHelper.votedSongs[levelId.Substring(0, 32)] = new SongVote(key, upvote ? VoteType.Upvote : VoteType.Downvote);
                                BeatSaverDownloaderHelper.SaveDownloaderConfig();
                            }
                        }; break;
                    case 403:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Read-only token";
                        }; break;
                    case 401:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Token not found";
                        }; break;
                    case 400:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Bad token";
                        }; break;
                    default:
                        {
                            _upvoteButton.interactable = true;
                            _downvoteButton.interactable = true;
                            _ratingText.text = "Error " + voteWWW.responseCode;
                        }; break;
                }
            }
        }


        public void SetTimer(float currentTime, float totalTime)
        {
            if (_timerText != null)
            {
                _timerText.text = SecondsToString(totalTime - currentTime);
            }

        }

        public string SecondsToString(float time)
        {
            int minutes = (int)(time / 60f);
            int seconds = (int)(time - minutes * 60);
            return minutes.ToString() + ":" + string.Format("{0:00}", seconds);
        }
    }
}
