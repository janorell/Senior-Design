using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using IndieStudio.Package.MoviePlayer;

///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

[CustomEditor (typeof( VideoManager))]
public class VideoManagerEditor :  Editor
{
		public override void OnInspectorGUI ()
		{
				SerializedObject attrib = new SerializedObject (target);
				VideoManager vm = (VideoManager)target;

				attrib.Update ();

				EditorGUILayout.Separator ();

                #if !(UNITY_5 || UNITY_2017 || UNITY_2018_0 || UNITY_2018_1 || UNITY_2018_2)
                    //Unity 2018.3 or higher
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = Colors.cyanColor;
                    EditorGUILayout.Separator();
                    EditorGUILayout.Separator();
                    if (GUILayout.Button("Apply", GUILayout.Width(70), GUILayout.Height(30), GUILayout.ExpandWidth(false)))
                    {
                        PrefabUtility.ApplyPrefabInstance(vm.gameObject, InteractionMode.AutomatedAction);
                    }
                    GUI.backgroundColor = Colors.whiteColor;
                    EditorGUILayout.EndHorizontal();
                #endif
				EditorGUILayout.Separator ();
				EditorGUILayout.HelpBox ("Use Video of codec H.264 or VP8 , and compress your video", MessageType.Info);

				GUI.backgroundColor = Colors.yellowColor;         
				if(GUILayout.Button("How to convert video codec",GUILayout.Width(200),GUILayout.Height(20))){
					Application.OpenURL("https://www.vlchelp.com/convert-video-format/");
				}
				GUI.backgroundColor = Colors.whiteColor;         

				EditorGUILayout.HelpBox ("Click on 'Add New Video Clip' to create new Video Clip item", MessageType.None);
				EditorGUILayout.HelpBox ("Click on 'Remove' to remove the Video Clip item", MessageType.None);
				EditorGUILayout.HelpBox ("Use 'Type' drop down list to select the type of the Video Clip item", MessageType.None);
				
				EditorGUILayout.HelpBox ("Important - Click on Apply button that located on the top to save your changes", MessageType.Info);

				EditorGUILayout.Separator ();

				GUILayout.BeginHorizontal ();
				GUI.backgroundColor = Colors.greenColor;         
				if (GUILayout.Button ("Add New Video Clip", GUILayout.Width (130), GUILayout.Height (20))) {
					vm.videoClips.Add (new VideoManager.MyVideoClip());
				}

				GUI.backgroundColor = Colors.paleGreen;
				if (GUILayout.Button ("More Assets", GUILayout.Width (110), GUILayout.Height (20))) {
					Application.OpenURL (Links.indieStudioStoreURL);
				}
				if (GUILayout.Button ("Contact US", GUILayout.Width (110), GUILayout.Height (20))) {
					Application.OpenURL (Links.indieStudioContactUsURL);
				}
				GUI.backgroundColor = Colors.whiteColor;
				GUILayout.EndHorizontal ();

				EditorGUILayout.Separator ();
				vm.showContents = EditorGUILayout.Foldout (vm.showContents, "Video Clips");

				if (vm.showContents)
						for (int i = 0; i < vm.videoClips.Count; i++) {
				
								EditorGUILayout.BeginHorizontal ();
								GUILayout.Space (10);
								EditorGUILayout.BeginVertical ();

								EditorGUILayout.Separator ();
								GUI.backgroundColor = Colors.redColor;         
								if (GUILayout.Button ("Remove", GUILayout.Width (70), GUILayout.Height (20))) {
										bool isOk = EditorUtility.DisplayDialog ("Confirm Message", "Are you sure that you want to remove the selected item ?", "yes", "no");

										if (isOk) {
												vm.videoClips.RemoveAt (i);
												return;
										}
								}
								GUI.backgroundColor = Colors.whiteColor;
								EditorGUILayout.Separator ();

								if (vm.videoClips [i].type == VideoManager.MyVideoClip.Type.BUILT_IN) {
									vm.videoClips [i].clip = EditorGUILayout.ObjectField ("Video Clip", vm.videoClips [i].clip, typeof(VideoClip), true) as VideoClip;
								} else if (vm.videoClips [i].type == VideoManager.MyVideoClip.Type.URL) {
									vm.videoClips [i].url = EditorGUILayout.TextField ("URL", vm.videoClips [i].url);
								}

								vm.videoClips [i].type = (VideoManager.MyVideoClip.Type) EditorGUILayout.EnumPopup ("Type",	vm.videoClips [i].type);

								EditorGUILayout.Separator ();

								EditorGUILayout.BeginHorizontal ();

								EditorGUI.BeginDisabledGroup (i == vm.videoClips.Count - 1);
								if (GUILayout.Button ("▼", GUILayout.Width (22), GUILayout.Height (22))) {
										MoveDown (i, vm);
								}
								EditorGUI.EndDisabledGroup ();

								EditorGUI.BeginDisabledGroup (i - 1 < 0);
								if (GUILayout.Button ("▲", GUILayout.Width (22), GUILayout.Height (22))) {
										MoveUp (i, vm);
								}
								EditorGUI.EndDisabledGroup ();

								EditorGUILayout.EndHorizontal ();

								EditorGUILayout.Separator ();
								GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height (2));
								
								EditorGUILayout.EndVertical ();
								EditorGUILayout.EndHorizontal ();
						}
				EditorGUILayout.Separator ();

				EditorGUILayout.PropertyField (attrib.FindProperty ("audioSource"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("videoRawImage"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("soundLevelSlider"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("videoSlider"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("videoClipNumber"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("videoTimeText"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("soundVolumeIcons"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("loopIcons"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("shuffleIcons"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("playIcon"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("pauseIcon"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("loopButtonImage"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("shuffleButtonImage"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("soundButtonImage"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("playButtonImage"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("loadingPanel"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("playOnStart"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("fullScreen"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("allowControls"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("aspectRatio"), true);
				EditorGUILayout.PropertyField (attrib.FindProperty ("videoEndReachedEvent"), true);

				attrib.ApplyModifiedProperties ();

				if (GUI.changed) {
						DirtyUtil.MarkSceneDirty ();
				}
		}

		private void MoveUp (int index, VideoManager vm)
		{
				VideoManager.MyVideoClip vc = vm.videoClips [index];
				vm.videoClips.RemoveAt (index);
				vm.videoClips.Insert (index - 1,vc);
		}

		private void MoveDown (int index, VideoManager vm)
		{
			VideoManager.MyVideoClip vc = vm.videoClips [index];
			vm.videoClips.RemoveAt (index);
			vm.videoClips.Insert (index+1,vc);
		}
}
