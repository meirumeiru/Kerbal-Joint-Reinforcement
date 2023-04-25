Kerbal Joint Reinforcement Next, v4.2
=====================================

Physics stabilizer plugin for Kerbal Space Program

Source available at: https://github.com/meirumeiru/Kerbal-Joint-Reinforcement

***************************************************
****** INSTALLING KERBAL JOINT REINFORCEMENT ******
***************************************************

Merge the GameData folder with the existing one in your KSP directory.  KSP will then load it as an add-on.
The source folder simply contains the source code (in C#) for the plugin.  If you didn't already know what it
was, you don't need to worry about it; don't copy it over.


**********************
****** FEATURES ******
**********************

-- Reinforce attach nodes

	- Identifies and reinforces joints that are too weak for the size of the connected parts

	- Identifies and reinforces those joints in a vessel that are too weak due to limitations of the game and
	  the game engine

-- Add additional joints

	- Optionally adds additional joints to dampen wobbly rockets

	- Optionally adds additional joints between strategically selected parts to reinforce any vessel, even up
	  to unrealistic levels if selected

-- Physics Easing

	- All parts and joints are strengthened during physics loading (coming off of rails) to prevent Kraken
	  attacks on ships

-- Launch Clamp Easing

	- Prevents launch clamps from shifting on load, which could destroy the vehicle on the pad

-- Anti-Drift

	- The system works actively against drifting parts.

-- Parameters can be tweaked in included config.xml file


********************************************
****** config.xml value documentation ******
********************************************

Location of the file

	Plugins\PluginData\KerbalJointReinforcementNext\config.xml

General Values

	mode	selected mode, possible values are

			Realistic (default if nothing is selected)

				Reinforce attach nodes and simply try to fix those joints that are weak but are not intended
				to be weak


			RealisticPlus

				Realistic and also adds weak extra joints to dampen unwanted wobbling of rockets.


			Rigid

				Realistic and also adds strong extra joints to dampen unwanted wobbling of rockets.

				This mode isn't very realistic anymore. But it helps if you want to fly somewhat "special"
				configurations.


			RigidPlus

				Makes all joints very strong, adds strong extra joints to dampen unwanted wobbling of rockets
				and adds additional joints from every part to its grand parent.

				Use this mode if you don't want to care about breaking joints. Everything will be as rigid as
				possible.


	A lot more parameters can be tweaked as well, but they are not intendet to be tweaked normally. You can
	find more about them in the forum and on github.


***********************
****** CHANGELOG ******
***********************

v4.2.x	-> new version of KJR, again a re-development of main parts of the mod

		   it has been rebuilt with the idea in mind to bring back realistic joint behaviour and not simply
		   add more joints to stiffen the ships up to unrealistic levels

v4.1.x	-> better version in which joints are not built to keep parts in the place where they currently are,
		   but where they should be according to the original positions this works against the part shift that
		   could be observed in earlier versions and especially together with robotic parts

v4.0.x	-> the new version of KJR, a complete re-development where just ideas are kept from old versions

v3.x.x	-> previous KJR versions
