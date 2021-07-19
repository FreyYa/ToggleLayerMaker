using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FreyYa
{
	public class ParameterLayerMakerUI : UnityEditor.EditorWindow
	{
		private GameObject baseAvatar;
		private string status;
		private string paramName;
		private AnimationClip turnOn;
		private AnimationClip turnOff;



		[UnityEditor.MenuItem("FreyYa/Toggle Layer Maker")]
		static void Open()
		{
			var window = UnityEditor.EditorWindow.GetWindow<ParameterLayerMakerUI>();
			window.Setup();
		}
		private void Setup()
		{
		}
		private void OnGUI()
		{
			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			UnityEditor.EditorGUILayout.LabelField("Avartar");
			baseAvatar = UnityEditor.EditorGUILayout.ObjectField(baseAvatar, typeof(GameObject), true) as GameObject;
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			UnityEditor.EditorGUILayout.LabelField("Parameter Name");
			paramName = UnityEditor.EditorGUILayout.TextField(paramName);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			UnityEditor.EditorGUILayout.LabelField("Toggle Animations");
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			UnityEditor.EditorGUILayout.LabelField("Turn On Animation");
			turnOn = UnityEditor.EditorGUILayout.ObjectField(turnOn, typeof(AnimationClip), true) as AnimationClip;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			UnityEditor.EditorGUILayout.LabelField("Turn Off Animation");
			turnOff = UnityEditor.EditorGUILayout.ObjectField(turnOff, typeof(AnimationClip), true) as AnimationClip;
			GUILayout.EndHorizontal();

			UnityEditor.EditorGUILayout.LabelField(status);
			if (GUILayout.Button("Make it!"))
			{
				MakeParameters();
			}

			GUILayout.EndVertical();
		}
		private void OnDisable()
		{

		}
		private void MakeParameters()
		{
			try
			{
				if (GetTypeByClassName("VRCAvatarDescriptor") != null)
				{
					var type = GetTypeByClassName("VRCAvatarDescriptor");
					VRC.SDK3.Avatars.Components.VRCAvatarDescriptor avatarDescriptor = (VRC.SDK3.Avatars.Components.VRCAvatarDescriptor)baseAvatar.GetComponent(type);

					LayerSetUp setup = new LayerSetUp();

					status = setup.SetupLayers(avatarDescriptor, paramName, turnOn, turnOff);
				}
			}
			catch (System.Exception ex)
			{
				status = ex.Message;
			}
		}

		/// <summary>
		/// UnityではType.GetTypeでクラスの型を名前で取得できないのでその代用
		/// </summary>
		/// <param name="className">クラス名</param>
		/// <returns>クラスの型</returns>
		public static System.Type GetTypeByClassName(string className)
		{
			foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (System.Type type in assembly.GetTypes())
				{
					if (type.Name == className)
					{
						return type;
					}
				}
			}
			return null;
		}
	}
}