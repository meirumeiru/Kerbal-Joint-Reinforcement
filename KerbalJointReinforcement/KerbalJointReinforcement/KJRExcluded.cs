namespace KerbalJointReinforcement
{
	// class to explicity exclude a part from handling by KJR
	
	/*
	 * to use it add this into your cfg file of the part
	 * 
	 * MODULE
	 * {
	 *     name = KJRExcluded
	 * }
	 */

	public class KJRExcluded : PartModule, IJointLockState
	{
		////////////////////////////////////////
		// IJointLockState

		bool IJointLockState.IsJointUnlocked()
		{
			return true;
		}
	}
}
