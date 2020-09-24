using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

namespace IndieStudio.Package.MoviePlayer
{
	public class IdleManager : MonoBehaviour
	{
		/// <summary>
		/// Whether the user is idle or not (any key clicked or not).
		/// </summary>
		private bool idle;

		/// <summary>
		/// Whether the idle method invoked or not.
		/// </summary>
		private bool idleInvoked;

		/// <summary>
		/// The last mouse position.
		/// </summary>
		private Vector2 lastPosition;

		/// <summary>
		/// The controls animator reference.
		/// </summary>
		public Animator controlsAnimator;

		[Range(1,10)]
		/// <summary>
		/// Set user idle after n seconds.
		/// </summary>
		public float setIdleAfter = 3;

        /// <summary>
        /// A static instance of this class
        /// </summary>
        public static IdleManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

		// Use this for initialization
		void Start ()
		{
			lastPosition = Input.mousePosition;
			idle = false;	
		}
		
		// Update is called once per frame
		void Update ()
		{
			if (VideoManager.instance != null) {
				if (!VideoManager.instance.fullScreen || !VideoManager.instance.allowControls) {
					return;
				}
            }
            else
            {
                return;
            }

			if (Input.anyKey || Input.GetMouseButton (0) || Vector2.Distance(Input.mousePosition,lastPosition) != 0) {
				if (idle) {
					ShowControls ();
				}
				idle = false;
				idleInvoked = false;
				CancelInvoke ("SetIdle");
			} else {
				if (!idle && !idleInvoked) {
					idleInvoked = true;
					Invoke ("SetIdle", setIdleAfter);
				}
			}

			lastPosition = Input.mousePosition;
		}

		/// <summary>
		/// Set status to idle.
		/// </summary>
		private void SetIdle ()
		{
			idle = true;
			HideControls ();
		}

		/// <summary>
		/// Hide the controls of the video player.
		/// </summary>
		private void HideControls ()
		{
			controlsAnimator.SetTrigger ("Hide");
		}

		/// <summary>
		/// Show the controls of the video player.
		/// </summary>
		private void ShowControls ()
		{
			controlsAnimator.SetTrigger ("Show");
		}

		void OnDestroy ()
		{
			CancelInvoke ();
		}
	}
}
