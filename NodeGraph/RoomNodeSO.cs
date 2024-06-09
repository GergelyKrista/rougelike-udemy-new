using System;
using System.Collections.Generic;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEditor;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector]public string id;
    [HideInInspector]public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector]public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    
    #region Editor Code

#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;
    
    /// <summary>
    /// Initialize node
    /// </summary>
    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;
        
        //Load room node type
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }
    
    /// <summary>
    /// Draw node with nodestyle
    /// </summary>
    public void Draw(GUIStyle nodeStyle)
    {
        GUILayout.BeginArea(rect, nodeStyle);
        
        EditorGUI.BeginChangeCheck();
        
        //if room node has a parent or is of type entrance than display a label else display a popup
        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            // Display label that can't be changed
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {

            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(this);
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Populate a string array with the room node tupes to display than can be selected
    /// </summary>
    private string[] GetRoomTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].name;
            }
        }

        return roomArray;
    }
    
    /// <summary>
    /// Process events for the room node
    /// </summary>
public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;
            
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            
            default:
                break;
                
        }
    }
    
    // Process ProcessMouseDownEvent
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // Left click
        if (currentEvent.button == 0)
        {
           ProcessLeftClickDownEvent(currentEvent);
        }
        // Right click
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }
    
    // Process left click event
    private void ProcessLeftClickDownEvent(Event currentEvent)
    {
        Selection.activeObject = this;
        
        //toggle node selection
        isSelected = !isSelected;
    }
    
    // Process right click event
    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }
    
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent(currentEvent);
        }
    }
    
    private void ProcessLeftClickUpEvent(Event currentEvent)
    {
        isLeftClickDragging = false;
    }
    
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }
    
    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        DragNode(currentEvent.delta);
        GUI.changed = true;    
    }
    
    private void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildRoomNodeIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
    }
    
    private bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        // Check if there is already a boss node connected in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
                isConnectedBossNodeAlready = true;
        }
        
        // If the child room is a boss room and there is already a boss room connected to the node graph return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
            return false;
        
        // If the child room has a type of none return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;
        
        // If the node already had a child with this child ID return false
        if (childRoomNodeIDList.Contains(childID))
            return false;
        
        // If this node ID and the child ID are the same return false
        if (id == childID)
            return false;
        
        // If this childID  is already in the parentID list return false
        if (parentRoomNodeIDList.Contains(childID))
            return false;
        
        // If the child node already has a parent return false
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
            return false;
        
        // if the child is a corridor and this node is a corridor return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;
        
        // If this child is not a corridor and this node is not a corridor return false 
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;
        
        // If adding a corridor check that this node has < the maximum permitted child corridors
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;
        
        // If the child toom is an entrance return false - the entrance must always be the top level parent node
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
            return false;
        
        // If adding a room to a corridor check that the this corridor node doesn't already have a room node added
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;
       
        return true;
    }
    
    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }
    
#endif

    #endregion
}
