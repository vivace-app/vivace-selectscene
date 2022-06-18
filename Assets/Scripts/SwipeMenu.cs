using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Project.Scripts.SelectScreen
{
    public class SwipeMenu : MonoBehaviour
    {
        public GameObject scrollbar;
        private float _scrollPos;
        private float _distance;

        private float[] _pos;

        // private AudioSource[] _AudioSource; //プレビュー楽曲情報格納
        ////////private AudioSource[] _fullAudioSource; //フル楽曲情報格納
        // private static float[] _musicTime; //楽曲の再生時間を格納
        // [FormerlySerializedAs("DisplayedMusicTime")] public Text displayedMusicTime; //画面に表示される楽曲の再生時間
        public RectTransform background;

        // [FormerlySerializedAs("ScrollView")] public RectTransform scrollView;
        // [FormerlySerializedAs("ScrollViewPadding")] public HorizontalLayoutGroup scrollViewPadding;
        public Text yourHighScoreText;
        public Text onlineHighScoreText;
        public GameObject cardTemplate;
        private ToggleGroup[] _toggleGroup;
        public static int selectedNumTmp;
        private string _selectedLevelTmp;

        private AudioClip[] _previewAudioClip;
        private AudioSource[] _previewAudioSource;

        //private List<DownloadProcessManager.MusicList> _musicData;
        private AssetBundle[] _assetBundle;

        // ------------------------------------------------------------------------------------

        private const string GetMyScoreApiUri = ""; //EnvDataStore.ApiUri + "/auth/myScore";
        private const string GetOnlineScoreApiUri = ""; //EnvDataStore.ApiUri + "/topScore";

        // ------------------------------------------------------------------------------------

        [Serializable]
        public class MyScoreResponse
        {
            public bool success;
            public List<ScoreList> data;
        }

        [Serializable]
        public class ScoreList
        {
            public string name;
            public int score;
        }

        private void Start()
        {
            const int len = 5;
            _pos = new float[len];
            _distance = 1f / (_pos.Length - 1f);
            for (var i = 0; i < _pos.Length; i++) _pos[i] = _distance * i;
            BackgroundCover();
        }

        /// pivot を中心に、target のScaleを変化させる
        public void ScaleAround(GameObject target, Vector3 pivot, Vector3 newScale)
        {
            Vector3 targetPos = target.transform.localPosition;
            Vector3 diff = targetPos - pivot;
            float relativeScale = newScale.x / target.transform.localScale.x;

            Vector3 resultPos = pivot + diff * relativeScale;
            target.transform.localScale = newScale;
            target.transform.localPosition = resultPos;
        }

        public void Update()
        {
            if (Input.GetMouseButton(0)) // クリック中は，scroll_posに横スクロールのx座標を格納し続ける．
                _scrollPos = scrollbar.GetComponent<Scrollbar>().value;
            else // クリックを離したときに，scroll_posの値を参考に，最も近いカードを中央に持ってくる．
                foreach (var t in _pos)
                    if (_scrollPos < t + _distance / 2 && _scrollPos > t - _distance / 2)
                        scrollbar.GetComponent<Scrollbar>().value =
                            Mathf.Lerp(scrollbar.GetComponent<Scrollbar>().value, t, 0.1f);

            for (var i = 0; i < _pos.Length; i++)
            {
                if (!(_scrollPos < _pos[i] + _distance / 2) || !(_scrollPos > _pos[i] - _distance / 2)) continue;

                Debug.Log(transform.GetChild(i).transform.position.y);
                // カードを拡大する
                transform.GetChild(i).localScale =
                    Vector2.Lerp(transform.GetChild(i).localScale, new Vector2(1f, 1f), 0.5f);
                transform.GetChild(i).transform.position = Vector2.Lerp(
                    transform.GetChild(i).transform.position,
                    new Vector2(transform.GetChild(i).transform.position.x,
                        560f), 0.5f);


                //SelectedMusic(i); //　楽曲再生の実行と停止を行う（1フレーム毎）

                // カードを縮小する
                for (var cnt = 0; cnt < _pos.Length; cnt++)
                {
                    if (i == cnt) continue;
                    transform.GetChild(cnt).localScale = Vector2.Lerp(transform.GetChild(cnt).localScale,
                        new Vector2(0.7f, 0.7f), 0.5f);
                    transform.GetChild(cnt).transform.position = Vector2.Lerp(
                        transform.GetChild(cnt).transform.position,
                        new Vector2(transform.GetChild(cnt).transform.position.x,
                            560f + 465f * 0.15f), 0.5f);
                }

                // TODO 左右の幅を揃えるやつ
                // for (var sub = 0; sub < _pos.Length; sub++)
                // {
                //     if (i == sub || i == sub - 1 || i == sub + 1) continue;
                //     transform.GetChild(sub).transform.position = Vector2.Lerp(
                //         transform.GetChild(sub).transform.position,
                //         new Vector2(transform.GetChild(i).transform.position.x - 465f * 0.85f * (i - sub), 
                //             transform.GetChild(sub).transform.position.y), 0.5f);
                // }
            }
        }

        /// <summary>
        /// 背景画像を画面いっぱいに広げます．
        /// </summary>
        private void BackgroundCover()
        {
            var scale = 1f;
            if (Screen.width < 1920)
                scale = 1.5f;
            if (Screen.width < Screen.height)
                scale = (float)(Screen.height * 16) / (Screen.width * 9);
            background.sizeDelta = new Vector2(Screen.width * scale, Screen.height * scale);
        }

        /// <summary>
        /// マイベストスコアを取得します．
        /// </summary>
        private IEnumerator MyScoreNetworkProcess(string selectedMusic, string selectedLevel)
        {
            var form = new WWWForm();
            form.AddField("token", PlayerPrefs.GetString("jwt"));
            form.AddField("music", selectedMusic);
            form.AddField("level", selectedLevel);
            var request = UnityWebRequest.Post(GetMyScoreApiUri, form);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    ApplyMyScore(request.downloadHandler.text);
                    break;

                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    yourHighScoreText.text = "--------";
                    break;

                case UnityWebRequest.Result.InProgress:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// マイベストスコアを設定します．
        /// </summary>
        private void ApplyMyScore(string data)
        {
            var jsnData = JsonUtility.FromJson<MyScoreResponse>(data);

            if (jsnData.success && jsnData.data.Count != 0)
                yourHighScoreText.text = jsnData.data[0].score.ToString();
            else
                yourHighScoreText.text = "--------";
        }

        /// <summary>
        /// 最高スコアを取得します．
        /// </summary>
        private IEnumerator OnlineScoreNetworkProcess(string selectedMusic, string selectedLevel)
        {
            var form = new WWWForm();
            form.AddField("music", selectedMusic);
            form.AddField("level", selectedLevel);
            var request = UnityWebRequest.Post(GetOnlineScoreApiUri, form);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    ApplyOnlineScore(request.downloadHandler.text);
                    break;

                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    onlineHighScoreText.text = "--------";
                    break;

                case UnityWebRequest.Result.InProgress:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 最高スコアを設定します．
        /// </summary>
        private void ApplyOnlineScore(string data)
        {
            var jsnData = JsonUtility.FromJson<MyScoreResponse>(data);

            if (jsnData.success && jsnData.data.Count != 0)
                onlineHighScoreText.text = jsnData.data[0].score.ToString();
            else
                onlineHighScoreText.text = "--------";
        }
    }
}