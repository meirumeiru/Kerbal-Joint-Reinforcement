using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	public struct LineDrawer
	{
		private LineRenderer lineRenderer;
		private float lineSize;

		public LineDrawer(float lineSize = 0.02f)
		{
			GameObject lineObj = new GameObject("LineObj");
			lineRenderer = lineObj.AddComponent<LineRenderer>();
			lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
			lineRenderer.material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

			this.lineSize = lineSize;
		}

		private void init(float lineSize = 0.02f)
		{
			if(lineRenderer == null)
			{
				GameObject lineObj = new GameObject("LineObj");
				lineRenderer = lineObj.AddComponent<LineRenderer>();
				lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
				lineRenderer.material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

				this.lineSize = lineSize;
			}
		}

		// draws lines through the provided vertices
		public void DrawLineInGameView(Vector3 start, Vector3 end, Color color)
		{
			if(lineRenderer == null)
			{
				init(0.02f);
			}

			lineRenderer.startColor = Color.Lerp(color, Color.black, 0.8f);
			lineRenderer.endColor = color;

			lineRenderer.startWidth = lineSize;
			lineRenderer.endWidth = lineSize;

			lineRenderer.SetPosition(0, start);
			lineRenderer.SetPosition(1, end);
		}

		public void Destroy()
		{
			if(lineRenderer != null)
			{
				UnityEngine.Object.Destroy(lineRenderer.gameObject);
			}
		}
	}

	class KJRAnalyzer
	{
		class item
		{
			public ConfigurableJoint joint;
			public LineDrawer line;
			public Color color;
		};

		class itemlist
		{
			public Vessel vessel;
			public List<item> items;
		}

		private static bool bShow = true;

		public static void OnLoad(bool p_bShow)
		{
			bShow = p_bShow;
		}

		public static bool Show
		{
			get { return bShow; }
			set
			{
				bShow = value;

				if(bShow)
				{
					for(int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
						WasModified(FlightGlobals.VesselsLoaded[i]);
				}
				else
				{
					while(lists.Count > 0)
						Clear(lists[0].vessel);
				}
			}
		}

		static List<itemlist> lists = new List<itemlist>();

		public static void LateUpdate()
		{
			if(!bShow)
				return;

			for(int i = 0; i < lists.Count; i++)
			{
				if(!lists[i].vessel.loaded)
					continue;

				for(int j = 0; j < lists[i].items.Count; j++)
				{
					try
					{
						lists[i].items[j].line.DrawLineInGameView(
							lists[i].items[j].joint.transform.position,
							lists[i].items[j].joint.connectedBody.transform.position,
							lists[i].items[j].color);
					}
					catch(Exception)
					{
						// how's that possible? ... well, anyway, it's just a debug-window... I'll search this later
						if(lists[i].items[j].joint == null)
						{
							lists[i].items[j].line.Destroy();
							lists[i].items.RemoveAt(j--);
						}
					}
				}
			}
		}

		public static void WasModified(Vessel v)
		{
			if(!bShow)
				return;

			Clear(v);

			KJRJointTracker mjm = KJRManager.Instance.GetJointTracker();

			itemlist l = new itemlist();
			l.vessel = v;
			l.items = new List<item>();

			foreach(Part p in v.Parts)
			{
				ConfigurableJoint[] joints = p.GetComponents<ConfigurableJoint>();

				for(int i = 0; i < joints.Length; i++)
				{
					item t = new item();
					
					t.joint = joints[i];

					switch(mjm.GetJointReason(t.joint))
					{
					case KJRJointTracker.Reason.None:
						if(!WindowManager.Instance.ShowKSPJoints)
							continue;

						t.color = Color.green;
						break;

					case KJRJointTracker.Reason.ReinforceInversions:
						if(!WindowManager.Instance.ShowReinforcedInversions)
							continue;

						t.color = Color.magenta;
						break;

					case KJRJointTracker.Reason.ExtraStabilityJoint:
						if(!WindowManager.Instance.ShowExtraStabilityJoints)
							continue;

						t.color = Color.yellow;
						break;
					}

					t.line = new LineDrawer();

					l.items.Add(t);
				}
			}

			if(l.items.Count > 0)
				lists.Add(l);
		}

		public static void Clear(Vessel v)
		{
			for(int i = 0; i < lists.Count; i++)
			{
				if(lists[i].vessel == v)
				{
					for(int j = 0; j < lists[i].items.Count; j++)
						lists[i].items[j].line.Destroy();

					lists.RemoveAt(i);
					return;
				}
			}
		}
	}

#endif
}
