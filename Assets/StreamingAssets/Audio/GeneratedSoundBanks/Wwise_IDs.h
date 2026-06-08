/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID START_BACKGROUNDSUBMARINEMFX = 3003027233U;
        static const AkUniqueID START_BUTTONPRESS = 738466431U;
        static const AkUniqueID START_PHOSPHORUSCAMERAFLASH_EVENT = 3214537693U;
        static const AkUniqueID START_TENSIONEVENT = 1403223316U;
        static const AkUniqueID STOP_BACKGROUNDSUBMARINEMFX = 116843463U;
        static const AkUniqueID STOP_TENSIONEVENT = 3884825202U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace PLAYER_STATE
        {
            static const AkUniqueID GROUP = 4071417932U;

            namespace STATE
            {
                static const AkUniqueID ALIVE = 655265632U;
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID STUNNED = 124361234U;
            } // namespace STATE
        } // namespace PLAYER_STATE

    } // namespace STATES

    namespace SWITCHES
    {
        namespace ENERGY_STATUS
        {
            static const AkUniqueID GROUP = 894977922U;

            namespace SWITCH
            {
                static const AkUniqueID HIGH_ENERGY = 154043800U;
                static const AkUniqueID LOW_ENERGY = 2073286940U;
                static const AkUniqueID NONE_ENERGY = 2623246582U;
            } // namespace SWITCH
        } // namespace ENERGY_STATUS

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID INCREMENTALTENSION = 3138205067U;
        static const AkUniqueID SUBMARINE_COLISION_DISTANCE = 511617432U;
    } // namespace GAME_PARAMETERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID GAMEPLAY_SFX = 3401228817U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID GENERAL_MFX = 54673317U;
        static const AkUniqueID GENERAL_SFX = 322129659U;
        static const AkUniqueID MASTER_AUDIO_BUS = 3803692087U;
        static const AkUniqueID SUBMARINE_MFX = 1957082925U;
        static const AkUniqueID SUBMARINE_SFX = 1690215283U;
    } // namespace BUSSES

    namespace AUX_BUSSES
    {
        static const AkUniqueID REVERB_AUX_SFX = 1714574586U;
        static const AkUniqueID REVERB_MUFFLED_MFX_AUX = 2315337392U;
    } // namespace AUX_BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
