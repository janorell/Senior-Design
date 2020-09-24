using UnityEngine;
using System.Collections;

///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

namespace IndieStudio.Package.MoviePlayer
{
	[DisallowMultipleComponent]
	public class UIEvents : MonoBehaviour
	{
            public void VideoCustomButtonClick(int index)
            {
                VideoManager.instance.SetUpVideoClip(index, true);
            }

			public void PlayButtonEvent ()
			{
					if (VideoManager.instance.IsPlaying && !VideoManager.instance.Interrupted) {
							VideoManager.instance.PauseVideoClip ();
					} else if (!VideoManager.instance.IsPlaying && VideoManager.instance.Interrupted) {
							VideoManager.instance.PlayVideoClip ();
					}
			}

			public void FullScreenButtonEvent ()
			{
				VideoManager.instance.ToggleFullScreen ();
			}

			public void StopButtonEvent ()
			{
					VideoManager.instance.StopVideoClip ();
			}

			public void SoundButtonEvent ()
			{
					if (VideoManager.instance.Muted) {
							VideoManager.instance.UnMuteVideoClip ();
					} else {
							VideoManager.instance.MuteVideoClip ();
					}
			}

			public void LoopButtonEvent ()
			{
					VideoManager.instance.ToggleLoop ();
			}

			public void ShuffleButtonEvent ()
			{
					VideoManager.instance.ToggleShuffle ();
			}

			public void NextVideoButtonEvent ()
			{
					VideoManager.instance.NextVideoClip ();
			}

			public void PreviousVideoButtonEvent ()
			{
					VideoManager.instance.PreviousVideoClip ();
			}

			public void SoundLevelSliderPotentialDrag ()
			{
					VideoManager.instance.UnMuteVideoClip ();
			}

			public void VideoSliderClick ()
			{
					if (VideoManager.instance.IsPlaying) {
							VideoManager.instance.clickBeganOnVideoSlider = false;
							VideoManager.instance.PlayVideoClipAtTime ((int)VideoManager.instance.videoSlider.value);
					}
			}

			public void VideoSliderPotentialDrag ()
			{
					VideoManager.instance.clickBeganOnVideoSlider = true;
			}

			public void VideoSliderEndDrag ()
			{
					if (VideoManager.instance.clickBeganOnVideoSlider) {
							VideoManager.instance.clickBeganOnVideoSlider = false;
							if (VideoManager.instance.IsPlaying) {
								VideoManager.instance.PlayVideoClipAtTime ((int)VideoManager.instance.videoSlider.value);
							}
					}
			}
	}
}