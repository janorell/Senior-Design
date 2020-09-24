using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Events;

///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

namespace IndieStudio.Package.MoviePlayer
{
	[RequireComponent (typeof(AudioSource))]
	[RequireComponent (typeof(UIEvents))]
	[RequireComponent (typeof(VideoPlayer))]
	[DisallowMultipleComponent]
	public class VideoManager : MonoBehaviour
	{
		/// <summary>
		/// The video player component.
		/// </summary>
		private UnityEngine.Video.VideoPlayer videoPlayer;

		/// <summary>
		/// The raw image of the video player in the UI canvas.
		/// </summary>
		public RawImage videoRawImage;

		/// <summary>
		/// The video clips list.
		/// </summary>
		public List<MyVideoClip> videoClips = new List <MyVideoClip> ();

		/// <summary>
		/// The audio source reference.
		/// </summary>
		public AudioSource audioSource;

		/// <summary>
		/// The total time of the video clip.
		/// </summary>
		private string totalTime = "00:00";

		/// <summary>
		/// The index of the current video clip.
		/// </summary>
		[HideInInspector]
		public int currentClipIndex;

		/// <summary>
		/// The length of the current video clip.
		/// </summary>
		private float currentVideoClipLength;

		/// <summary>
		/// Whether a click down on the video player slider.
		/// </summary>
		private bool videoPlayerSliderClickDown;

		/// <summary>
		/// Whether the video player is interrupted or not.
		/// </summary>
		private bool interrupted;

		/// <summary>
		/// Whether the video player is muted or not.
		/// </summary>
		private bool muted;

		/// <summary>
		/// The slider of the sound level.
		/// </summary>
		public Slider soundLevelSlider;

		/// <summary>
		/// The slider of the video player.
		/// </summary>
		public Slider videoSlider;

		/// <summary>
		/// The text number of the video clip.
		/// </summary>
		public Text videoClipNumber;

		/// <summary>
		/// The text of the video's time.
		/// </summary>
		public Text videoTimeText;

		/// <summary>
		/// The current video clip.
		/// </summary>
		[HideInInspector]
		public VideoClip currentVideoClip;

		/// <summary>
		/// The sound volume icons.
		/// Contains a set of volume icons from the highest level to the lowest level
		/// </summary>
		public Sprite[] soundVolumeIcons;

		/// <summary>
		/// The loop icons.
		/// Contains two sprites for the loop button (OFF and ON)
		/// </summary>
		public Sprite[] loopIcons;

		/// <summary>
		/// The shuffle icons.
		/// Contains two sprites for the shuffle button (OFF and ON)
		/// </summary>
		public Sprite[] shuffleIcons;

		/// <summary>
		/// The play icon reference.
		/// </summary>
		public Sprite playIcon;

		/// <summary>
		/// The pause icon reference.
		/// </summary>
		public Sprite pauseIcon;

		/// <summary>
		/// The loop button image reference.
		/// </summary>
		public Image loopButtonImage;

		/// <summary>
		/// The shuffle button image reference.
		/// </summary>
		public Image shuffleButtonImage;

		/// <summary>
		/// The sound button image reference.
		/// </summary>
		public Image soundButtonImage;

		/// <summary>
		/// The play button image reference.
		/// </summary>
		public Image playButtonImage;

		/// <summary>
		/// The loading panel reference.
		/// </summary>
		public GameObject loadingPanel;

		/// <summary>
		/// Whether to play the first video clip on start or not.
		/// </summary>
		public bool playOnStart = true;

		/// <summary>
		/// Whether the click began on music slider or not.
		/// </summary>
		[HideInInspector]
		public bool clickBeganOnVideoSlider;

		/// <summary>
		/// Used for editor.
		/// </summary>
		[HideInInspector]
		public bool showContents;

		/// <summary>
		/// The aspect ratio of the video.
		/// </summary>
		public VideoAspectRatio aspectRatio;

		/// <summary>
		/// Whether the video player in full screen mode or not.
		/// </summary>
		public bool fullScreen;

		/// <summary>
		/// Whether to allow video player controls or not.
		/// </summary>
		public bool allowControls = true;

		/// <summary>
		/// Whether the video player loop is enabled or not
		/// </summary>
		private bool loop = true;

		/// <summary>
		/// Whether to shuffle playing video clips or not.
		/// </summary>
		private bool shuffle;

		/// <summary>
		/// The TV image reference.
		/// </summary>
		private Image tvImage;

		/// <summary>
		/// The background image.
		/// </summary>
		private Image background;

		/// <summary>
		/// Whether to enable video slider follow or not.
		/// </summary>
		private bool videoSliderFollowEnabled = true;

		/// <summary>
		/// The video end reached event.
		/// </summary>
		public UnityEvent videoEndReachedEvent;

		/// <summary>
		/// The static instance of this class.
		/// </summary>
		public static VideoManager instance;

		void Awake ()
		{
			if (instance == null) {
				instance = this;
			}
		}

		void Start ()
		{
			//Setting up the references
			Application.runInBackground = true;
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			if (audioSource == null) {
				audioSource = GetComponent<AudioSource> ();
			}

			if (soundLevelSlider == null) {
				soundLevelSlider = GameObject.Find ("SoundLevelSlider").GetComponent<Slider> ();
			}

			if (videoSlider == null) {
				videoSlider = GameObject.Find ("VideoSlider").GetComponent<Slider> ();
			}

			//Find TVImage
			GameObject tempBg = GameObject.Find ("TVImage");
			if (tempBg != null)
				tvImage = tempBg.GetComponent<Image> ();

			//Find Screen Background
			tempBg = GameObject.Find ("ScreenBackground");
			if (tempBg != null)
				background = tempBg.GetComponent<Image> ();

			if (audioSource == null) {
				Debug.LogWarning ("Undefined AudioSource Reference");
			}

			if (!allowControls) {
				GameObject.Find ("Controls").gameObject.SetActive (false);
			}

			//Set sound level slider boundary
			soundLevelSlider.minValue = 0;
			soundLevelSlider.maxValue = 1;
			SetLoopIcon ();

			videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer> ();

			videoPlayer.aspectRatio = aspectRatio;

			videoPlayer.skipOnDrop = true;

			videoPlayer.prepareCompleted += PrepareCompleted;
			videoPlayer.seekCompleted += SeekCompleted;
			videoPlayer.errorReceived += ErrorReceived;

			//Set Audio Output to AudioSource
			videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			videoPlayer.controlledAudioTrackCount = 1;
			videoPlayer.SetTargetAudioSource (0, audioSource);

			//Set the initial video clip
			if (shuffle) {
				SetUpRandomVideoClip ();
			} else {
				SetUpVideoClip (0, playOnStart);
			}

			SetUpFullScreenMode ();
		}

		void Update ()
		{
			if (videoPlayer == null) {
				return;
			}

			if (videoSliderFollowEnabled) {
				//Set time of the audio clip
				SetTextTime ((float)videoPlayer.time, totalTime);
			}

			//Set the sound volume
			audioSource.volume = soundLevelSlider.value;

			if (videoPlayer.isPlaying && videoSliderFollowEnabled) {
				if (!clickBeganOnVideoSlider) {
					SetVideoSliderValue (videoPlayer.time);
				}
			}

			//automatically go to the next video clip ,when the current video clip is fnished
			if (!Interrupted && !clickBeganOnVideoSlider && Mathf.Abs (currentVideoClipLength - videoSlider.value) <= 1f) {
				videoEndReachedEvent.Invoke ();

				//On Video Finished Event
				NextVideoClip ();
			}
		}


		/// <summary>
		/// Set the value of video's slider.
		/// </summary>
		private void SetVideoSliderValue (double value)
		{
			videoSlider.value = (float)value;
		}

		/// <summary>
		/// Set the audio clip.
		/// </summary>
		/// <param name="videoClip">The Audio clip.</param>
		public void SetAudioClip (VideoClip videoClip)
		{
			this.currentVideoClip = videoClip;
		}

		/// <summary>
		/// Play the audio clip at time.
		/// </summary>
		/// <param name="time">time in seconds.</param>
		public void PlayVideoClipAtTime (long time)
		{
			if (time >= currentVideoClipLength) {
				NextVideoClip ();
				return;
			}

			ShowLoadingPanel ();
			playButtonImage.sprite = pauseIcon; 
			interrupted = false;
			videoPlayer.time = time;
			videoSliderFollowEnabled = false;
		}

		/// <summary>
		/// Play the current video clip.
		/// </summary>
		public void PlayVideoClip ()
		{
			if (!videoPlayer.isPrepared) {
				videoPlayer.Prepare ();
			} else {
				OnVideoPrepared ();
			}
		}

		/// <summary>
		/// Pause the video clip.
		/// </summary>
		public void PauseVideoClip ()
		{
			playButtonImage.sprite = playIcon;
			interrupted = true;
			videoPlayer.Pause ();
			//PlayList.instance.SetPlayIcon (currentClipIndex);
		}

		/// <summary>
		/// Stop the video clip.
		/// </summary>
		public void StopVideoClip ()
		{
			interrupted = true;
			playButtonImage.sprite = playIcon;
			videoPlayer.Pause ();
			videoPlayer.time = 0;
			SetVideoSliderValue (0);
		}

		/// <summary>
		/// Mute the video clip.
		/// </summary>
		public void MuteVideoClip ()
		{
			soundButtonImage.sprite = soundVolumeIcons [3];
			muted = true;
			audioSource.mute = true;
		}

		/// <summary>
		/// Unmute the video clip.
		/// </summary>
		public void UnMuteVideoClip ()
		{
			soundButtonImage.sprite = soundVolumeIcons [0];
			muted = false;
			audioSource.mute = false;
		}

		/// <summary>
		/// Toggle playing videos as a loop.
		/// </summary>
		public void ToggleLoop ()
		{
			loop = !loop;
			SetLoopIcon ();
		}

		/// <summary>
		/// Toggle the shuffle feature.
		/// </summary>
		public void ToggleShuffle ()
		{
			shuffle = !shuffle;
			SetShuffleIcon ();
		}

		/// <summary>
		/// Set the loop icon.
		/// </summary>
		private void SetLoopIcon ()
		{
			if (loop) {
				loopButtonImage.sprite = loopIcons [1];
			} else {
				loopButtonImage.sprite = loopIcons [0];
			}
		}

		/// <summary>
		/// Set the shuffle icon.
		/// </summary>
		private void SetShuffleIcon ()
		{
			if (shuffle) {
				shuffleButtonImage.sprite = shuffleIcons [1];
			} else {
				shuffleButtonImage.sprite = shuffleIcons [0];
			}
		}

		/// <summary>
		///Play the next video clip
		/// </summary>
		public void NextVideoClip ()
		{
			interrupted = false;

			if (shuffle) {
				SetUpRandomVideoClip ();
			} else {
				if (loop) {
					if (currentClipIndex == videoClips.Count - 1) {
						SetUpVideoClip (0, !interrupted);
						return;
					} 
				}

				if (currentClipIndex + 1 < videoClips.Count) {
					SetUpVideoClip (currentClipIndex + 1, !interrupted);
				} else {
					StopVideoClip ();
				}
			}
		}

		/// <summary>
		/// Play the previous video clip.
		/// </summary>
		public void PreviousVideoClip ()
		{
			interrupted = false;

			if (shuffle) {
				SetUpRandomVideoClip ();
			} else {
				if (loop) {
					if (currentClipIndex == 0) {
						SetUpVideoClip (videoClips.Count - 1, !interrupted);
						return;
					} 
				}

				if (currentClipIndex - 1 >= 0) {
					SetUpVideoClip (currentClipIndex - 1, !interrupted);
				}
			}
		}

		/// <summary>
		/// Set up the video clip of the given index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="playClip">If set to true , then play the clip.</param>
		public void SetUpVideoClip (int index, bool playClip)
		{
			if (!(index >= 0 && index < videoClips.Count)) {
				return;
			}
		
			//PlayList.instance.UnSelectItem (currentClipIndex);
			ShowLoadingPanel ();
			
			StopVideoClip ();
			currentClipIndex = index;

			videoClipNumber.text = (index + 1) + "/" + videoClips.Count;

			if (videoClips [index].type == MyVideoClip.Type.URL) {
				videoPlayer.source = VideoSource.Url;
				videoPlayer.url = videoClips [index].url;
			} else {
				videoPlayer.source = VideoSource.VideoClip;
				currentVideoClip = videoClips [index].clip;
				OnAssignVideoClip ((float)currentVideoClip.length);
			}
				
			if (playClip) {
				PlayVideoClip (); 
			}
			//PlayList.instance.SelectItem (index);
		}


		/// <summary>
		/// On assign video clip
		/// </summary>
		private void OnAssignVideoClip (float videoClipLength)
		{
			videoSlider.minValue = 0;
			videoSlider.maxValue = videoClipLength;
			totalTime = CommonUtil.TimeToString (videoClipLength);
			currentVideoClipLength = videoClipLength;
			if (currentVideoClip != null) {
				videoPlayer.clip = currentVideoClip;
			}
		}

		/// <summary>
		/// Set up random video clip.
		/// </summary>
		private void SetUpRandomVideoClip ()
		{				
			int index = GetRandomVideoClipIndex ();
		
			if (index != -1) {
				SetUpVideoClip (index, !interrupted);
			} else {
				StopVideoClip ();
			}
		}

		/// <summary>
		/// Get a random video clip index.
		/// </summary>
		/// <returns>The random video clip index.</returns>
		public int GetRandomVideoClipIndex ()
		{
			int index = -1;
			List<int> indexes = new List<int> ();
			for (int i = 0; i < videoClips.Count; i++) {
				if (currentClipIndex == i)
					continue;
				indexes.Add (i);
			}

			if (indexes.Count != 0)
				index = indexes [Random.Range (0, indexes.Count)];

			return index;
		}

		void OnDestroy ()
		{
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
		}

		/// <summary>
		/// Set the video's time in the text.
		/// </summary>
		/// <param name="time">Time.</param>
		/// <param name="totalTime">Total time.</param>
		public void SetTextTime (float time, string totalTime)
		{
			if (videoTimeText == null) {
				return;
			}

			videoTimeText.text = CommonUtil.TimeToString (time) + " / " + totalTime;
		}

		/// <summary>
		/// Toggles the full screen mode.
		/// </summary>
		public void ToggleFullScreen ()
		{
			fullScreen = !fullScreen;
			SetUpFullScreenMode ();
		}

		/// <summary>
		/// Change to full screen mode if the fullScreen flag is enabled otherwise use the default.
		/// </summary>
		private void SetUpFullScreenMode ()
		{
			if (fullScreen) {
				videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
				if (Camera.main != null) {
					videoPlayer.targetCamera = Camera.main;
					if (background != null)
						background.enabled = false;
					if (tvImage != null)
						tvImage.enabled = false;
					if (videoRawImage != null)
						videoRawImage.enabled = false;
				} else {
					Debug.LogWarning ("Main Camera is not found in the scene");
				}
			} else {
                IdleManager.instance.CancelInvoke();

				videoPlayer.renderMode = VideoRenderMode.APIOnly;
				if (background != null)
					background.enabled = true;
				if (tvImage != null)
					tvImage.enabled = true;
				if (videoRawImage != null)
					videoRawImage.enabled = true;
			}
		}

		/// <summary>
		/// On video prepared, play the video clip.
		/// </summary>
		private void OnVideoPrepared ()
		{
			videoRawImage.texture = videoPlayer.texture;
			playButtonImage.sprite = pauseIcon;
			interrupted = false;
			videoSliderFollowEnabled = true;
			clickBeganOnVideoSlider = false;
			videoPlayer.Play ();
			audioSource.Play ();
			//PlayList.instance.SetPauseIcon (currentClipIndex);
		}

		/// <summary>
		/// On Video Player Prepare is completed.
		/// </summary>
		/// <param name="videoPlayer">Video player.</param>
		private void PrepareCompleted (VideoPlayer videoPlayer)
		{
			if (videoPlayer == null) {
				return;
			}

			Debug.Log ("Video is Prepared sucessfully");

			if (videoPlayer.source == VideoSource.Url) {
				OnAssignVideoClip (videoPlayer.frameCount / videoPlayer.frameRate);
			}

			HideLoadingPanel ();

			OnVideoPrepared ();
		}

		/// <summary>
		/// On Video Player Seeek is completed.
		/// </summary>
		/// <param name="videoPlayer">Video player.</param>
		private void SeekCompleted (VideoPlayer videoPlayer)
		{
			Debug.Log ("Seek operation is completed");
			HideLoadingPanel ();
			videoSliderFollowEnabled = true;
		}

		/// <summary>
		/// On Video error received.
		/// </summary>
		/// <param name="videoPlayer">Video player.</param>
		/// <param name="message">Error message.</param>
		private void ErrorReceived (VideoPlayer videoPlayer, string message)
		{
			Debug.Log (message);
		}

		/// <summary>
		/// On Video the player end reached event.
		/// </summary>
		public void VideoPlayerEndReached ()
		{
			Debug.Log ("Video End Reached");
		}

		/// <summary>
		/// Show the loading panel.
		/// </summary>
		private void ShowLoadingPanel ()
		{
			if (loadingPanel != null)
				loadingPanel.SetActive (true);
		}

		/// <summary>
		/// Hide the loading panel.
		/// </summary>
		private void HideLoadingPanel ()
		{
			if (loadingPanel != null)
				loadingPanel.SetActive (false);
		}

		public bool Interrupted {
			get { return this.interrupted; }
		}

		public bool IsPlaying {
			get{ return videoPlayer != null ? videoPlayer.isPlaying : false; }
		}

		public bool Muted {
			get{ return this.muted; }
		}

		public bool isLoop {
			get{ return this.loop; }
		}

		[System.Serializable]
		public class MyVideoClip
		{
			public bool showContents = true;
			public VideoClip clip;
			public string url;
			public Sprite thumbnail;
			public Type type;

			public enum Type
			{
				BUILT_IN,
				URL}
			;
		}
	}
}