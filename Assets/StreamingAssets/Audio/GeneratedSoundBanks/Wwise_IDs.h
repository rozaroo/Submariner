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
        static const AkUniqueID START_CLOSING_PRESSURE_DOORSFX = 378872923U;
        static const AkUniqueID START_LEVERPULLFINISHED = 2469108041U;
        static const AkUniqueID START_OPENING_PRESSURE_DOORSFX = 4235702204U;
        static const AkUniqueID START_PHOSPHORUSCAMERAFLASH_EVENT = 3214537693U;
        static const AkUniqueID START_PLAYERFOOTSTEPS = 2940474702U;
        static const AkUniqueID START_SONARPINGINNER = 1735885581U;
        static const AkUniqueID START_SONARPINGOUTER = 997126444U;
        static const AkUniqueID START_TENSIONEVENT = 1403223316U;
        static const AkUniqueID STOP_BACKGROUNDSUBMARINEMFX = 116843463U;
        static const AkUniqueID STOP_TENSIONEVENT = 3884825202U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace ENERGY_STATUS
        {
            static const AkUniqueID GROUP = 894977922U;

            namespace STATE
            {
                static const AkUniqueID EMPTY = 3354297748U;
                static const AkUniqueID FULL = 2510516222U;
                static const AkUniqueID LOW = 545371365U;
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace ENERGY_STATUS

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
        namespace PLAYER_FOOTSTEPSSURFACE
        {
            static const AkUniqueID GROUP = 1044191727U;

            namespace SWITCH
            {
                static const AkUniqueID METAL = 2473969246U;
                static const AkUniqueID WATER = 2654748154U;
            } // namespace SWITCH
        } // namespace PLAYER_FOOTSTEPSSURFACE

        namespace WATER_LEVEL
        {
            static const AkUniqueID GROUP = 290589761U;

            namespace SWITCH
            {
                static const AkUniqueID HIGH = 3550808449U;
                static const AkUniqueID LOW = 545371365U;
            } // namespace SWITCH
        } // namespace WATER_LEVEL

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
        static const AkUniqueID PLAYER_SFX = 817096458U;
        static const AkUniqueID SUBMARINE_MFX = 1957082925U;
        static const AkUniqueID SUBMARINE_SFX = 1690215283U;
    } // namespace BUSSES

    namespace AUX_BUSSES
    {
        static const AkUniqueID PLAYER_REVERB_AUX_SFX = 2070985102U;
        static const AkUniqueID REVERB_MUFFLED_MFX_AUX = 2315337392U;
        static const AkUniqueID SUBMARINE_REVERB_AUX_SFX = 3031736565U;
    } // namespace AUX_BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
