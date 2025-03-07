﻿using System;
using System.Collections;
using UnityEngine;

/*
	all credit on this one goes to CYBUTEK
*/

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	[RequireComponent(typeof(CanvasGroup))]
	public class CanvasGroupFader : MonoBehaviour
	{
		private CanvasGroup _CanvasGroup;
		private IEnumerator _FadeCoroutine;

		public bool IsFading
		{
			get
			{
				return _FadeCoroutine != null;
			}
		}

		// fades the canvas group to a specified alpha using the supplied blocking state during fade with optional callback
		public void FadeTo(float alpha, float duration, Action callback = null)
		{
			if(_CanvasGroup == null)
				return;

			Fade(_CanvasGroup.alpha, alpha, duration, callback);
		}

		// sets the alpha value of the canvas group
		public void SetAlpha(float alpha)
		{
			if(_CanvasGroup == null)
				return;

			alpha = Mathf.Clamp01(alpha);
			_CanvasGroup.alpha = alpha;
		}

		protected virtual void Awake()
		{
			// cache components
			_CanvasGroup = GetComponent<CanvasGroup>();
		}

		// starts a fade from one alpha value to another with callback
		private void Fade(float from, float to, float duration, Action callback)
		{
			if(_FadeCoroutine != null)
				StopCoroutine(_FadeCoroutine);

			_FadeCoroutine = FadeCoroutine(from, to, duration, callback);
			StartCoroutine(_FadeCoroutine);
		}

		// coroutine that handles the fading
		private IEnumerator FadeCoroutine(float from, float to, float duration, Action callback)
		{
			// wait for end of frame so that only the last call to fade that frame is honoured
			yield return new WaitForEndOfFrame();

			float progress = 0.0f;

			while(progress <= 1.0f)
			{
				progress += Time.deltaTime / duration;
				SetAlpha(Mathf.Lerp(from, to, progress));
				yield return null;
			}

			if(callback != null)
				callback.Invoke();

			_FadeCoroutine = null;
		}
	}

#endif
}
