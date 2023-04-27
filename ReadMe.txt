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

-- Settings integrated into stock UI

-- Physics easing

    - All parts and joints are strengthened during physics loading (coming off of rails) to prevent Kraken
      attacks on ships

-- Launch clamp easing

    - Prevents launch clamps from shifting on load, which could destroy the vehicle on the pad

-- Anti-Drift

    - The system works actively against drifting parts.

-- Parameters can be tweaked in config.xml file


********************************************
****** config.xml value documentation ******
********************************************

Location of the file

    Plugins\PluginData\KerbalJointReinforcementNext\config.xml


Preset Values

    The presets consist of 3 values separated by ';' and stored as string. The strings are named "Easy", "Normal",
	"Moderate" and "Hard" and represent the presets for every difficulty level.
    The values of the presets are:

    Type    Name                    Default Value        Action

    bool    reinforceAttachNodes    1                    Identifies and reinforces joints that are too weak for the size of the connected parts
    bool    reinforceInversions     1                    Identifies and reinforces those joints in a vessel that are too weak due to limitations of the game and the game engine
    int     extraLevel              0                    Level for extra joints used (see 'Extra Level' for more information)

General Values (used as default setting, when no preset is selected)

    Type    Name                    Default Value        Action

    bool    reinforceAttachNodes    1                    Identifies and reinforces joints that are too weak for the size of the connected parts
    bool    reinforceInversions     1                    Identifies and reinforces those joints in a vessel that are too weak due to limitations of the game and the game engine
    int     extraLevel              0                    Level for extra joints used (see 'Extra Level' for more information)

    bool    reinforceLaunchClamps   0                    Toggles stiffening of launch clamp connections

    bool    debug                   0                    Toggles debug output to log

Joint Strength Values

    Type    Name                    Default Value        Action

    bool    useVolumeNotArea        1                    Switches to calculating connection area based on volume, not area; not technically correct, but allows a better approximation of very large rockets
    float   massForAdjustment       0.01                 Parts below this mass will not be stiffened

    float   breakForceMultiplier    4                    Factor scales the failure strength (for forces) of joint connections
    float   breakTorqueMultiplier   4                    Factor scales the failure strength (for torque) of joint connections
    float   breakStrengthPerArea    1500                 Overrides above values if not equal to 1; joint strength is based on the area of the part and failure strength is equal to this value times connection area
    float   breakTorquePerMOI       6000                 Same as above value, but for torques rather than forces and is based on the moment of inertia, not area

    float   inversionMassFactor     2                    Connections with mass ratios above this factor ('inversion') will be reinforced with an additional joint
    float   solutionMassFactor      2                    Joints added to solve an 'inversion' must have a mass ratio below this factor, if no solution is found, no joint is created

Extra Joint Strength Values

    Type    Name                    Default Value        Action

    float   extraLinearForceW       10                  Force of the created weak joints (translational)
    float   extraLinearSpringW      1E+20               Spring strength of the created weak joints (translational)
    float   extraLinearDamperW      0                   Damping of the created weak joints (translational)

    float   extraLinearForce        1E+20               Force of the created strong joints (translational)
    float   extraLinearSpring       1E+20               Spring strength of the created strong joints (translational)
    float   extraLinearDamper       0                   Damping of the created strong joints (translational)

    float   extraAngularForceW      10                  Force of the created weak joints (rotational)
    float   extraAngularSpringW     60000               Spring strength of the created weak joints (rotational)
    float   extraAngularDamperW     0                   Damping of the created weak joints (rotational)

    float   extraAngularForce       1E+20               Force of the created strong joints (rotational)
    float   extraAngularSpring      60000               Spring strength of the created strong joints (rotational)
    float   extraAngularDamper      0                   Damping of the created strong joints (rotational)

    float   extraBreakingForce      -1                  Failure strength (for forces) of created joints (-1 = max value)
    float   extraBreakingTorque     -1                  Failure strength (for forces) of created joints (-1 = max value)


Extra Level Descriptions

    Value   Description

    0       Does not add extra joints

    1       Adds weak joints to dampen unwanted wobbling (realistic)

    2       Adds strong joints to dampen wobbling (less realistic)

    3       Adds a lot of very strong joints to eliminate all movement between parts (unrealistic)


***********************
****** CHANGELOG ******
***********************

v4.2.x  -> new version of KJR, again a re-development of main parts of the mod

           it has been rebuilt with the idea in mind to bring back realistic joint behaviour and not simply
           add more joints to stiffen the ships up to unrealistic levels

v4.1.x  -> better version in which joints are not built to keep parts in the place where they currently are,
           but where they should be according to the original positions this works against the part shift that
           could be observed in earlier versions and especially together with robotic parts

v4.0.x  -> the new version of KJR, a complete re-development where just ideas are kept from old versions

v3.x.x  -> previous KJR versions
