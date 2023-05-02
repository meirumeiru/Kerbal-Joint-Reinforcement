using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalJointReinforcement
{
    // class to handle rotating docking ports

    class KJRDockingNode : PartModule, IJointLockState
	{
		private ModuleDockingNode dock = null;

		internal static void InitializePart(Part part)
		{
			try
			{
				ModuleDockingNode dock = part.FindModuleImplementing<ModuleDockingNode>();

				if(dock)
				{
					KJRDockingNode module = part.GetComponent<KJRDockingNode>();

					if(!module)
					{
						module = part.gameObject.AddComponent<KJRDockingNode>();
						module.dock = dock;

						module.OnCreate();
					}
				}
			}
			catch(Exception)
			{ }
		}

		public void OnCreate()
        {
			dock.Fields["nodeIsLocked"].OnValueModified += ModifyLocked;
		}

		public void OnDestroy()
		{
			dock.Fields["nodeIsLocked"].OnValueModified -= ModifyLocked;
		}

		protected void ModifyLocked(object obj)
		{
			if(HighLogic.LoadedSceneIsFlight)
				KJRManager.CycleAllAutoStrut(part.vessel);
		}

		////////////////////////////////////////
		// IJointLockState

		bool IJointLockState.IsJointUnlocked()
		{
			return !dock.nodeIsLocked;
		}
	}
}
