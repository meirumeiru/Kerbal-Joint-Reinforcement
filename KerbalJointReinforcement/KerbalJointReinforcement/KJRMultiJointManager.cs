using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalJointReinforcement
{
	// all this class exists to do is to act as a box attached to 
	// for a sequence of three parts (A, B, C), connected in series, this will exist on B and hold the strengthening joint from A to C
	// if the joint from A to B or B to C is broken, this will destroy the joint A to C and then destroy itself

	internal class KJRMultiJointManager
	{
		internal enum Reason
		{
			None, ReinforceDecoupler, ReinforceLaunchClamp, AdditionalJointToParent, MultiPartJointTreeChildren, MultiPartJointTreeChildrenRoot, ReinforceInversions
		};

		internal struct ConfigurableJointWithInfo
		{
			internal ConfigurableJoint joint;
			internal bool direct;
		}

		internal List<Part> linkedSet;

		private List<Part> tempPartList;

		private Dictionary<Part, List<ConfigurableJointWithInfo>> multiJointDict;
		private Dictionary<ConfigurableJoint, Reason> jointReasonDict;

		public KJRMultiJointManager()
		{
			multiJointDict = new Dictionary<Part, List<ConfigurableJointWithInfo>>();
			linkedSet = new List<Part>();
			tempPartList = new List<Part>();
			jointReasonDict = new Dictionary<ConfigurableJoint,Reason>();
		}

		// creates a list of all parts that form a path/connection
		// from part1 to part2 and stores it in linkedSet

		public bool TrySetValidLinkedSet(Part part1, Part part2)
		{
			linkedSet.Clear();
			tempPartList.Clear();

			while(part1 != null)
			{
				linkedSet.Add(part1);
				part1 = KJRJointUtils.IsJointAdjustmentAllowed(part1) ? part1.parent : null;
			}

			while(part2 != null)
			{
				tempPartList.Add(part2);
				part2 = KJRJointUtils.IsJointAdjustmentAllowed(part2) ? part2.parent : null;
			}

			int i = linkedSet.Count - 1;
			int j = tempPartList.Count - 1;

			if(linkedSet[i] != tempPartList[j])
				return false; // not same root, so they can never be in a valid set

			while((i >= 0) && (j >= 0) && (linkedSet[i] == tempPartList[j]))
			{ --i; --j; }

			if(linkedSet.Count > i + 2)
				linkedSet.RemoveRange(i + 2, linkedSet.Count - i - 2);
			linkedSet.RemoveAt(0);

			if((tempPartList.Count > 1) && (j > 0))
				linkedSet.AddRange(tempPartList.GetRange(1, j)); 

			return linkedSet.Count > 1;
		}

		public void RegisterMultiJoint(Part part, ConfigurableJoint joint, bool direct, Reason jointReason)
		{
			List<ConfigurableJointWithInfo> configJointList;

			if(multiJointDict.TryGetValue(part, out configJointList))
			{
				for(int i = configJointList.Count - 1; i >= 0; --i)
					if(configJointList[i].joint == null)
						configJointList.RemoveAt(i);
			}
			else
				multiJointDict.Add(part, configJointList = new List<ConfigurableJointWithInfo>());

			configJointList.Add(new ConfigurableJointWithInfo(){ joint = joint, direct = direct });

			if(!jointReasonDict.ContainsKey(joint))
				jointReasonDict.Add(joint, jointReason);
		}

		public bool CheckDirectJointBetweenParts(Part part1, Part part2)
		{
			if(part1 == null || part2 == null || part1 == part2)
				return false;

			List<ConfigurableJointWithInfo> configJointList;

			if(!multiJointDict.TryGetValue(part1, out configJointList))
				return false;

			Rigidbody part2Rigidbody = part2.Rigidbody;

			for(int i = 0; i < configJointList.Count; i++)
			{
				if(configJointList[i].direct)
				{
					if((configJointList[i].joint.GetComponent<Rigidbody>() == part2Rigidbody)
					|| (configJointList[i].joint.connectedBody == part2Rigidbody))
						return true;
				}
			}

			return false;
		}
/*
		public bool CheckIndirectJointBetweenParts(Part part1, Part part2)
		{
			if(part1 == null || part2 == null || part1 == part2)
				return false;

			List<ConfigurableJointWithInfo> configJointList;

			if(!multiJointDict.TryGetValue(part1, out configJointList))
				return false;

			Rigidbody part2Rigidbody = part2.Rigidbody;

			for(int i = 0; i < configJointList.Count; i++)
			{
				if((configJointList[i].joint.GetComponent<Rigidbody>() == part2Rigidbody)
				|| (configJointList[i].joint.connectedBody == part2Rigidbody))
					return true;
			}

			return false;
		}
*/
		public void RemoveAllVesselJoints(Vessel v)
		{
			if(v.loaded)
			{
				List<Part> toRemove = new List<Part>();

				foreach(var e in multiJointDict)
				{
					if(e.Key.vessel == v)
					{
						foreach(ConfigurableJointWithInfo jointWI in e.Value)
						{
							jointReasonDict.Remove(jointWI.joint);

							if(jointWI.joint != null)
								GameObject.Destroy(jointWI.joint);
						}

						toRemove.Add(e.Key);
					}
				}

				foreach(Part part in toRemove)
					multiJointDict.Remove(part);
			}
		}

		public void RemovePartJoints(Part part)
		{
			if(part == null)
				return;

			List<ConfigurableJointWithInfo> jointList;
			if(multiJointDict.TryGetValue(part, out jointList))
			{
				foreach(ConfigurableJointWithInfo jointWI in jointList)
				{
					jointReasonDict.Remove(jointWI.joint);

					if(jointWI.joint != null)
						GameObject.Destroy(jointWI.joint);
				}

				multiJointDict.Remove(part);
			}
		}

#if IncludeAnalyzer
		internal ConfigurableJoint[] GetAllJoints()
		{
			HashSet<ConfigurableJoint> l = new HashSet<ConfigurableJoint>();

			Dictionary<Part, List<ConfigurableJointWithInfo>>.Enumerator e = multiJointDict.GetEnumerator();

			while(e.MoveNext())
			{
				List<ConfigurableJointWithInfo> j = e.Current.Value;

				for(int a = 0; a < j.Count; a++)
				{
					if(j[a].direct)
						l.Add(j[a].joint);
				}
			}

			return l.ToArray();
		}
#endif

		internal Reason GetJointReason(ConfigurableJoint joint)
		{
			Reason jointReason;
			if(jointReasonDict.TryGetValue(joint, out jointReason))
				return jointReason;

			return Reason.None;
		}
	}
}
