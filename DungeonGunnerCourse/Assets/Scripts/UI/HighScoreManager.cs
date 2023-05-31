using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;

public class HighScoreManager : SingletonMonobehaviour<HighScoreManager>
{
    private HighScores highScores = new HighScores();
    public string addScoreURL = 
        "https://oceansofterra.com/score-api/addScore.php?";
    public string highscoreURL = 
         "https://oceansofterra.com/score-api/display.php";
    public string getRankURL = 
         "https://oceansofterra.com/score-api/getRank.php?";
    public GameObject highScoreUI;
    private string secretKey = "mySecretKey";
    private int scoreRank = 0;

    protected override async void Awake()
    {
        base.Awake();
        LoadScores();
        // int rank = await GetScoreRank(10);
        // int result = GetRank(10);
    }

    /// <summary>
    /// Load Scores From Disk
    /// </summary>

    public async Task<string> CallAPI(string api_url)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(api_url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else
            {
                Debug.Log("There was an error getting the high score: ");
                return null;
            }
        }
    }

    private IEnumerator<object> GetScoresAPI()
    {
        UnityWebRequest hs_get = UnityWebRequest.Get(highscoreURL);
        yield return hs_get.SendWebRequest();
        if (hs_get.error != null) {
            Debug.Log("There was an error posting the high score: " 
                    + hs_get.error);
        }
        else {
            string dataText = hs_get.downloadHandler.text;

            MatchCollection mc = Regex.Matches(dataText, @"_");
            ClearScoreList();
            if (mc.Count > 0)
            {
                string[] splitData = Regex.Split(dataText, @"_");
                Score itemScore = new Score();
                for (int i = 0; i < mc.Count; i++)
                {
                    if (i % 3 == 0)  {
                        itemScore.playerName = splitData[i];
                    }
                    else if(i % 3 == 1) {
                        itemScore.playerScore = Convert.ToInt64(splitData[i]);
                    }
                    else {
                        itemScore.levelDescription = splitData[i];
                        highScores.scoreList.Add(itemScore);
                        itemScore = new Score();
                    }
                }
            }
            highScoreUI.GetComponent<DisplayHighScoresUI>().DisplayScores();
        }
    }

    private async void LoadScores()
    {
        StartCoroutine(GetScoresAPI());
        // string post_url = highscoreURL;
        // string dataText = await CallAPI(post_url);

        // MatchCollection mc = Regex.Matches(dataText, @"_");
        // ClearScoreList();
        // if (mc.Count > 0)
        // {
        //     string[] splitData = Regex.Split(dataText, @"_");
        //     Score itemScore = new Score();
        //     for (int i = 0; i < mc.Count; i++)
        //     {
        //         if (i % 3 == 0)  {
        //             itemScore.playerName = splitData[i];
        //         }
        //         else if(i % 3 == 1) {
        //             itemScore.playerScore = Convert.ToInt64(splitData[i]);
        //         }
        //         else {
        //             itemScore.levelDescription = splitData[i];
        //             highScores.scoreList.Add(itemScore);
        //             itemScore = new Score();
        //         }
        //     }
        // }
        // highScoreUI.GetComponent<DisplayHighScoresUI>().DisplayScores();
    }

    /// <summary>
    /// Clear All Scores
    /// </summary>
    private void ClearScoreList()
    {
        highScores.scoreList.Clear();
    }

    /// <summary>
    /// Add score to high scores list
    /// </summary>
    public void AddScore(Score score, int rank)
    {
        highScores.scoreList.Insert(rank - 1, score);

        // Maintain the maximum number of scores to save
        if (highScores.scoreList.Count > Settings.numberOfHighScoresToSave)
        {
            highScores.scoreList.RemoveAt(Settings.numberOfHighScoresToSave);
        }

        StartCoroutine (PostScores(score));
        // SaveScores();
    }

    /// <summary>
    /// Save Scores To Disk
    /// </summary>
    private void SaveScores()
    {

        BinaryFormatter bf = new BinaryFormatter();

        FileStream file = File.Create(Application.persistentDataPath + "/DungeonGunnerHighScores.dat");

        bf.Serialize(file, highScores);
        file.Close();
    }

    private IEnumerator<object> PostScores(Score score)
    {
        string hash = HashInput(score.playerName + score.playerScore + score.levelDescription + secretKey);
        string post_url = addScoreURL + "playerName=" + 
            UnityWebRequest.EscapeURL(score.playerName) + "&playerScore=" 
            + score.playerScore + "&levelDescription=" + score.levelDescription + "&hash=" + hash;
        UnityWebRequest hs_post = UnityWebRequest.Post(post_url, hash);
        yield return hs_post.SendWebRequest();
        if (hs_post.error != null)
            Debug.Log("There was an error posting the high score: " 
                    + hs_post.error);
    }

    public string HashInput(string input)
    {
        SHA256Managed hm = new SHA256Managed();
        byte[] hashValue = 	
                hm.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
        string hash_convert = 
                BitConverter.ToString(hashValue).Replace("-", "").ToLower();
        return hash_convert;
    }

    /// <summary>
    /// Get highscores
    /// </summary>
    public HighScores GetHighScores()
    {
        return highScores;
    }

    public async Task<int> GetScoreRank(long gameScore)
    {
        string post_url = getRankURL + "&playerScore=" + gameScore;
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(post_url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return Convert.ToInt32(result);
            }
            else
            {
                Debug.LogError($"API call failed with status code {response.StatusCode}");
                return 0;
            }
        }
    }
}