using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNodeType_", menuName = "Scriptable Objects/Dungeon/Room Node Type")]
public class RoomNodeTypeSO : ScriptableObject
{
    public string roomNodeTypeName;
    
    #region DisplayInNodeGraphEditor Setting
    public bool displayInNodeGraphEditor = true;
    #endregion

    #region Corridor Setting
    public bool isCorridor;
    #endregion

    #region CorridorNS Setting
    public bool isCorridorNS;
    #endregion

    #region CorridorEW Setting
    public bool isCorridorEW;
    #endregion

    #region Entrance Setting
    public bool isEntrance;
    #endregion

    #region BossRoom Setting
    public bool isBossRoom;
    #endregion

    #region None Setting
    public bool isNone;
    #endregion

    #region Validation
#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckEmptyString(this, nameof(roomNodeTypeName), roomNodeTypeName);
    }
#endif   
    #endregion
}
