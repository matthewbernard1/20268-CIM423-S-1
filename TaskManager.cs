using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AirbnbTurnover
{
    public enum TaskId { None = -1, Trash, Couch, Dishes, Towels, Stain, Bed, Gloves }

    [Serializable]
    public class CleaningTask
    {
        public TaskId id;
        public string room;
        public string title;
        [TextArea] public string funnyMessage;
        public int required = 1;
        [NonSerialized] public int progress;
        public bool IsComplete => progress >= required;
    }

    /// <summary>
    /// Owns the game flow: intro screen, task checklist, funny messages, final inspection and restart.
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        public CleaningTask[] tasks;

        [Header("Screens")]
        public GameObject introPanel;
        public GameObject hudPanel;
        public GameObject endPanel;

        [Header("HUD")]
        public TMP_Text checklistText;
        public TMP_Text timerText;
        public TMP_Text messageText;
        public GameObject messageBubble;
        public Button finishButton;
        public float messageDuration = 6f;

        [Header("Story")]
        public float deadlineSeconds = 600f;

        float timeLeft;
        float messageHideTime;
        float nextGloveReminder;
        bool started;
        bool finished;
        AudioSource audioSource;
        AudioClip popClip;
        AudioClip dingClip;

        void Awake()
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            popClip = MakeTone("pop", new[] { 660f }, 0.08f);
            dingClip = MakeTone("ding", new[] { 784f, 1046f, 1318f }, 0.18f);
        }

        void Start()
        {
            timeLeft = deadlineSeconds;
            introPanel.SetActive(true);
            hudPanel.SetActive(false);
            endPanel.SetActive(false);
            messageBubble.SetActive(false);
            RefreshChecklist();
        }

        void Update()
        {
            if (messageBubble.activeSelf && Time.time > messageHideTime)
                messageBubble.SetActive(false);

            if (!started || finished)
                return;

            timeLeft -= Time.deltaTime;
            if (timeLeft > 0f)
            {
                var t = TimeSpan.FromSeconds(timeLeft);
                timerText.text = $"Guests arrive in <b>{t.Minutes}:{t.Seconds:00}</b>";
            }
            else
            {
                timerText.text = "<color=#FF6B6B>The guests are in the driveway. Act natural.</color>";
            }
        }

        // ---- Button callbacks -------------------------------------------------------

        public void StartCleaning()
        {
            started = true;
            introPanel.SetActive(false);
            hudPanel.SetActive(true);
            ShowMessage("First, grab the <b>latex gloves</b> to put them on. Then start in the <b>living room</b>: toss trash in a bin.");
        }

        public void FinishInspection()
        {
            if (!AllComplete())
            {
                ShowMessage("The inspector squints at you. Some chores are still unfinished.");
                return;
            }
            finished = true;
            hudPanel.SetActive(false);
            messageBubble.SetActive(false);
            endPanel.SetActive(true);
            audioSource.PlayOneShot(dingClip);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---- Progress ---------------------------------------------------------------

        public CleaningTask Get(TaskId id) => Array.Find(tasks, t => t.id == id);

        public void AddProgress(TaskId id, int amount = 1)
        {
            var task = Get(id);
            if (task == null || task.IsComplete)
                return;

            task.progress = Mathf.Min(task.required, task.progress + amount);
            if (task.IsComplete)
            {
                audioSource.PlayOneShot(dingClip);
                ShowMessage($"<color=#7CFC9A><b>{task.title} complete!</b></color>\n{task.funnyMessage}");
                if (AllComplete())
                    ShowMessage($"<color=#7CFC9A><b>{task.title} complete!</b></color>\n{task.funnyMessage}\n\n<b>Everything is clean. Press Finish Inspection!</b>");
            }
            RefreshChecklist();
        }

        public bool AllComplete() => Array.TrueForAll(tasks, t => t.IsComplete);

        /// <summary>Everything except the gloves stays locked until the gloves are on (or if there is no gloves task).</summary>
        public bool GlovesOn => Get(TaskId.Gloves)?.IsComplete ?? true;

        /// <summary>Told when the player reaches for something before putting the gloves on. Throttled so it doesn't spam.</summary>
        public void RemindGloves()
        {
            if (!started || finished || Time.time < nextGloveReminder)
                return;
            nextGloveReminder = Time.time + 4f;
            ShowMessage("Whoa, bare hands? Put on the <b>latex gloves</b> on the dining table first.");
        }

        public void PlayPop(Vector3 _)
        {
            audioSource.PlayOneShot(popClip, 0.6f);
        }

        public void ShowMessage(string text)
        {
            messageText.text = text;
            messageBubble.SetActive(true);
            messageHideTime = Time.time + messageDuration;
        }

        void RefreshChecklist()
        {
            var sb = new StringBuilder();
            string room = null;
            foreach (var t in tasks)
            {
                if (t.room != room)
                {
                    room = t.room;
                    sb.Append(sb.Length > 0 ? "\n" : "").Append("<size=80%><color=#9FB3C8>").Append(room.ToUpper()).Append("</color></size>\n");
                }
                if (t.IsComplete)
                    sb.Append("<color=#7CFC9A>[x]</color> <s>").Append(t.title).Append("</s>\n");
                else
                    sb.Append("[  ] ").Append(t.title).Append(t.required > 1 ? $"  <color=#FFD27F>{t.progress}/{t.required}</color>" : "").Append('\n');
            }
            checklistText.text = sb.ToString();
            finishButton.interactable = AllComplete();
        }

        static AudioClip MakeTone(string name, float[] notes, float noteLength)
        {
            const int rate = 44100;
            int perNote = Mathf.RoundToInt(rate * noteLength);
            var data = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Exp(-t * 18f);
                    data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * env * 0.5f;
                }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
