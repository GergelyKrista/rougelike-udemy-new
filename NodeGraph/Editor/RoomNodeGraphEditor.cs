// Import necessary namespaces
using System;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine.XR;

// Define the RoomNodeGraphEditor class, which is a type of EditorWindow
public class RoomNodeGraphEditor : EditorWindow
{
    // Define the style for the room nodes
    private GUIStyle roomNodeStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;
    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    // Define constants for node dimensions and padding
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 12;
    
    private const float connectingLineWidth = 3f;

    // Define a menu item for opening the Room Node Graph Editor window
    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room NodeGraph Editor")]
    private static void OpenWindow()
    {
        // Open the Room Node Graph Editor window
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    // Define what happens when the Room Node Graph Editor window is enabled
    private void OnEnable()
    {
        // Define the node layout style
        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        // Get the RoomNodeTypeListSO from the GameResources
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    [OnOpenAsset(0)] // Need the namesapce UnityEditor.Callbacks
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;
        if (roomNodeGraph != null)
        {
            OpenWindow();

            currentRoomNodeGraph = roomNodeGraph;

            return true;
        }

        return false;
    }

    private void OnGUI()
    {
        if (currentRoomNodeGraph != null)
        {
            // Draw line if being dragged
            DrawDraggedLine();
            
            // Process events
            ProcessEvents(Event.current);

            // Draw the room nodes
            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }
    
    private void DrawDraggedLine()
    {
        if (currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            //Draw line from room node to line position
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
                currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.white, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        // Check if the current room node is null or if the left mouse button is not being dragged
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }
        
        //if mouse isn't over room node or we are currently dragging a line from the room node then process room graph events
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            // Process room node graph events
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        else
        {
            // Process room node events
            currentRoomNode.ProcessEvents(currentEvent);
        
        }
        
    }
    
    /// <summary>
    /// Check to see if the mouse is over a room node - if so return the room node else return null
    /// </summary>
    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Process Room Node Graph Events
    /// </summary>
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            // Process mouse down event
            case EventType.MouseDown:
                ProcessMouseDown(currentEvent);
                break;
            
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            
            // Process mouse drag event
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            default:
                break;
        }
    }
    private void ProcessMouseDown(Event currentEvent)
    {
        // Check if the right mouse button is clicked
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
    }
    /// <summary>
    /// Show Context Menu
    /// </summary>
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.ShowAsContext();
    }

    private void CreateRoomNode(object mousePositionObject)
    {
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x=> x.isNone));
    }
    
    //process mouse drag event
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
    }
    
    // Process right mouse drag event - draw line from room node
    private void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }
    
    public void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }
    
    
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        // create room node scriptable object
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();
        
        // add room node to current room node graph room node list
        currentRoomNodeGraph.roomNodeList.Add(roomNode);
        
        // set room node values
        roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);
        
        // add room node to room node graph scriptable object asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        
        AssetDatabase.SaveAssets();
    }
    
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            //Check if over a room node
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);
            
            if (roomNode != null)
            {
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
                {
                    roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }
            
            ClearLineDrag();
        }
    }
    
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }
    
    /// <summary>
    /// Draw Room Nodes in the graph window
    /// </summary>
    private void DrawRoomNodes()
    {
        // Loop through all room nodes and draw them
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.Draw(roomNodeStyle);
        }
        
        GUI.changed = true;
    }
}    
        
