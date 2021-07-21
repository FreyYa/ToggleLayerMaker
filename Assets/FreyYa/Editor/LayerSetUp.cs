using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace FreyYa
{
	public class LayerSetUp
	{
		public string SetupLayers(VRCAvatarDescriptor avatarDescriptor, string paramName, AnimationClip turnOn, AnimationClip turnOff)
		{
			UnityEditor.Animations.AnimatorController FX = new UnityEditor.Animations.AnimatorController();
			VRCExpressionParameters paramFile = new VRCExpressionParameters();
			VRCExpressionsMenu mainMenuFile = new VRCExpressionsMenu();

			//FX레이어 탐지
			bool fxFind = false;
			for (int i = 0; i < avatarDescriptor.baseAnimationLayers.Length; i++)
			{
				var test = avatarDescriptor.baseAnimationLayers[i];
				switch (test.type)
				{
					case VRCAvatarDescriptor.AnimLayerType.FX:
						var tempFX = avatarDescriptor.baseAnimationLayers[i];
						if (!tempFX.isDefault)
						{
							fxFind = true;
						}
						else
						{
							fxFind = false;
						}
						if (tempFX.animatorController.ToString() == "null")
						{
							fxFind = false;
						}
						else
						{
							fxFind = true;
						}
						FX = avatarDescriptor.baseAnimationLayers[i].animatorController as UnityEditor.Animations.AnimatorController;
						break;
				}
			}
			if (!fxFind)
			{
				return "FX Layer Not Found!";
			}
			//파라메터 파일 탐지
			if (avatarDescriptor.expressionParameters != null)
			{
				paramFile = avatarDescriptor.expressionParameters;
			}
			else
			{
				//생성하던지 없다고 경고
				return "Expression Parameter File Not Found!";
			}
			//메뉴 탐지
			if (avatarDescriptor.expressionsMenu != null)
			{
				mainMenuFile = avatarDescriptor.expressionsMenu;
			}
			else
			{
				//생성하던지 없다고 경고
				return "Expression Menu File Not Found!";
			}

			if(turnOn == null)
			{
				return "Turn On AnimationClip File Not Found!";
			}
			if (turnOff == null)
			{
				return "Turn Off AnimationClip File Not Found!";
			}

			//동일한 이름의 파라메터가 있는지 확인하고 추가
			var resultParamName = FX.MakeUniqueParameterName(paramName);
			var resultLayerName = FX.MakeUniqueLayerName(paramName);

			//VRC 파라메터 추가
			if (paramFile.FindParameter(resultParamName) == null)
			{
				//파라메터가 없으면 추가 진행한다
				var cost = paramFile.CalcTotalCost();
				if (cost > 128)
				{
					if (!TryAddParam(paramFile, resultParamName))
					{
						return "Cannot add VRC parameter (Cost is max)";
					}
				}
				else
				{
					//파라메터 추가 시도
					var currentParamList = paramFile.parameters.ToList();
					int totalCost = 0;
					foreach (var parameter in currentParamList)
					{
						switch (parameter.valueType)
						{
							case VRCExpressionParameters.ValueType.Int:
							case VRCExpressionParameters.ValueType.Float:
								totalCost += 8;
								break;
							case VRCExpressionParameters.ValueType.Bool:
								totalCost += 1;
								break;
						}
					}
					if (totalCost + 1 > 128)
					{
						//하나 더 추가할 용량이 없을 시 시도
						if (!TryAddParam(paramFile, resultParamName))
						{
							return "Cannot add VRC parameter (Cost is max)";
						}
					}
					VRCExpressionParameters.Parameter param = new VRCExpressionParameters.Parameter();
					param.name = resultParamName;
					param.valueType = VRCExpressionParameters.ValueType.Bool;
					param.defaultValue = 0f;
					param.saved = false;
					currentParamList.Add(param);
					paramFile.parameters = currentParamList.ToArray();
				}
			}
			else
			{
				paramFile.FindParameter(resultParamName).valueType = VRCExpressionParameters.ValueType.Bool;
				paramFile.FindParameter(resultParamName).defaultValue = 0f;
			}

			FX.AddParameter(resultParamName, AnimatorControllerParameterType.Bool);

			//https://forum.unity.com/threads/animatorcontroller-addlayer-doesnt-create-default-animatorstatemachine.307873/
			UnityEditor.Animations.AnimatorControllerLayer targetLayer = new UnityEditor.Animations.AnimatorControllerLayer//이 부분이 문제인 것으로 보임
			{
				name = resultLayerName,
				defaultWeight = 1f,
			};
			var currentStatemachine = new AnimatorStateMachine();
			currentStatemachine.name = targetLayer.name;
			targetLayer.stateMachine = currentStatemachine;
			targetLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

			if (AssetDatabase.GetAssetPath(FX) != "")
				AssetDatabase.AddObjectToAsset(targetLayer.stateMachine, AssetDatabase.GetAssetPath(FX));

			FX.AddLayer(targetLayer);

			//State 생성
			var nullState = FX.layers.Last().stateMachine.AddState("null");

			var turnonState = FX.layers.Last().stateMachine.AddState(turnOn.name);
			turnonState.motion = turnOn;
			turnonState.writeDefaultValues = true;

			var turnoffState = FX.layers.Last().stateMachine.AddState(turnOff.name);
			turnoffState.motion = turnOff;
			turnoffState.writeDefaultValues = true;

			//트렌지션 생성
			//https://answers.unity.com/questions/1023907/how-can-i-create-a-animatorstatetransition-conditi.html

			var nullToTurnOnTrans = nullState.AddTransition(turnonState);
			var nullToTurnOffTrans = nullState.AddTransition(turnoffState);

			nullToTurnOnTrans.hasExitTime = false;
			nullToTurnOnTrans.exitTime = 0;
			nullToTurnOnTrans.duration = 0;

			nullToTurnOnTrans.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, resultParamName);

			nullToTurnOffTrans.hasExitTime = false;
			nullToTurnOffTrans.exitTime = 0;
			nullToTurnOffTrans.duration = 0;

			nullToTurnOffTrans.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, resultParamName);

			var offToOnTrans = turnoffState.AddTransition(turnonState);
			var onToOffTrans = turnonState.AddTransition(turnoffState);

			offToOnTrans.hasExitTime = false;
			offToOnTrans.exitTime = 0;
			offToOnTrans.duration = 0;

			offToOnTrans.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, resultParamName);

			onToOffTrans.hasExitTime = false;
			onToOffTrans.exitTime = 0;
			onToOffTrans.duration = 0;

			onToOffTrans.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, resultParamName);

			return "Success!";
		}

		private bool TryAddParam(VRCExpressionParameters paramFile, string paramName)
		{
			bool setParameter = false;
			//1차. 비어있는 파라메터 이름을 찾아본다
			for (int i = 0; i < paramFile.parameters.Length; i++)
			{
				//있는 파라메터를 수정
				if (paramFile.parameters[i].name == "")
				{
					paramFile.parameters[i].name = paramName;
					paramFile.parameters[i].valueType = VRCExpressionParameters.ValueType.Bool;
					paramFile.parameters[i].defaultValue = 0f;
					paramFile.parameters[i].saved = false;

					setParameter = true;
					break;
				}
			}

			return setParameter;
		}
	}
}