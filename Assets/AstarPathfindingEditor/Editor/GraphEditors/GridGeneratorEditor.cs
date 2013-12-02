using UnityEngine;
using UnityEditor;
using Pathfinding;
using System.Collections;

[CustomGraphEditor (typeof(GridGraph),"Grid Graph")]
public class GridGraphEditor : GraphEditor, ISerializableGraphEditor {
	
	bool locked = true;
	float newNodeSize;
	
	public bool showExtra = false;
	
	public int IntField (string label, int value, int offset, int adjust, out Rect r, out bool selected) {
		
		GUIStyle intStyle = EditorStyles.numberField;
		
		GUILayout.BeginHorizontal ();
		GUILayout.Space (15*EditorGUI.indentLevel);
		Rect r1 = GUILayoutUtility.GetRect (new GUIContent (label),intStyle);
		
		Rect r2 = GUILayoutUtility.GetRect (new GUIContent (value.ToString ()),intStyle);
		
		GUILayout.EndHorizontal ();
		
		
		r2.width += (r2.x-r1.x);
		r2.x = r1.x+offset;
		r2.width -= offset+offset+adjust;
		
		r = new Rect ();
		r.x = r2.x+r2.width;
		r.y = r1.y;
		r.width = offset;
		r.height = r1.height;
		
		GUI.SetNextControlName ("IntField_"+label);
		value = EditorGUI.IntField (r2,"",value);
		
		bool on = GUI.GetNameOfFocusedControl () == "IntField_"+label;
		selected = on;
		
		if (Event.current.type == EventType.Repaint) {
			
			
			
			intStyle.Draw (r1,new GUIContent (label),false,false,false,on);
			
		}
		
		return value;
	}
	
	public override void OnInspectorGUI (NavGraph target) {
		
		GridGraph graph = target as GridGraph;
		
		//GUILayout.BeginHorizontal ();
		//GUILayout.BeginVertical ();
		Rect lockRect;
		Rect tmpLockRect;
		
		GUIStyle lockStyle = AstarPathEditor.astarSkin.FindStyle ("GridSizeLock");
		if (lockStyle == null) {
			lockStyle = new GUIStyle ();
		}
		
		bool sizeSelected1 = false;
		bool sizeSelected2 = false;
		int newWidth = IntField ("Width (nodes)",graph.width,50,0, out lockRect, out sizeSelected1);
		int newDepth = IntField ("Depth (nodes)",graph.depth,50,0, out tmpLockRect, out sizeSelected2);
		
		//Rect r = GUILayoutUtility.GetRect (0,0,lockStyle);
		
		lockRect.width = lockStyle.fixedWidth;
		lockRect.height = lockStyle.fixedHeight;
		lockRect.x += lockStyle.margin.left;
		lockRect.y += lockStyle.margin.top;
		
		locked = GUI.Toggle (lockRect,locked,new GUIContent ("","If the width and depth values are locked, changing the node size will scale the grid which keeping the number of nodes consistent instead of keeping the size the same and changing the number of nodes in the graph"),lockStyle);
		
		//GUILayout.EndHorizontal ();
		
		
		
		if (newWidth != graph.width || newDepth != graph.depth) {
			SnapSizeToNodes (newWidth,newDepth,graph);
		}
		
		GUI.SetNextControlName ("NodeSize");
		newNodeSize = EditorGUILayout.FloatField ("Node size",graph.nodeSize);
		
		newNodeSize = newNodeSize <= 0.01F ? 0.01F : newNodeSize;
		
		//if ((GUI.GetNameOfFocusedControl () != "NodeSize" && Event.current.type == EventType.Repaint) || Event.current.keyCode == KeyCode.Return) {
			
			//Debug.Log ("Node Size Not Selected " + Event.current.type);
			
			if (graph.nodeSize != newNodeSize) {
				if (!locked) {
					graph.nodeSize = newNodeSize;
					Matrix4x4 oldMatrix = graph.matrix;
					graph.GenerateMatrix ();
					if (graph.matrix != oldMatrix) {
						//Rescann the graphs
						//AstarPath.active.AutoScan ();
						GUI.changed = true;
					}
				} else {
					float delta = newNodeSize / graph.nodeSize;
					graph.nodeSize = newNodeSize;
					graph.unclampedSize = new Vector2 (newWidth*graph.nodeSize,newDepth*graph.nodeSize);
					Vector3 newCenter = graph.matrix.MultiplyPoint3x4 (new Vector3 ((newWidth/2F)*delta,0,(newDepth/2F)*delta));
					graph.center = newCenter;
					graph.GenerateMatrix ();
					
					//Make sure the width & depths stay the same
					graph.width = newWidth;
					graph.depth = newDepth;
					AstarPath.active.AutoScan ();
				}
			}
		//}
		
		Vector3 pivotPoint;
		Vector3 diff;
		
		GUILayout.BeginHorizontal ();
		
		switch (pivot) {
			case GridPivot.Center:
				graph.center = EditorGUILayout.Vector3Field ("Center",graph.center);
				break;
			case GridPivot.TopLeft:
				pivotPoint = graph.matrix.MultiplyPoint3x4 (new Vector3 (0,0,graph.depth));
				diff = pivotPoint-graph.center;
				pivotPoint = EditorGUILayout.Vector3Field ("Top-Left",pivotPoint);
				graph.center = pivotPoint-diff;
				break;
			case GridPivot.TopRight:
				pivotPoint = graph.matrix.MultiplyPoint3x4 (new Vector3 (graph.width,0,graph.depth));
				diff = pivotPoint-graph.center;
				pivotPoint = EditorGUILayout.Vector3Field ("Top-Right",pivotPoint);
				graph.center = pivotPoint-diff;
				break;
			case GridPivot.BottomLeft:
				pivotPoint = graph.matrix.MultiplyPoint3x4 (new Vector3 (0,0,0));
				diff = pivotPoint-graph.center;
				pivotPoint = EditorGUILayout.Vector3Field ("Bottom-Left",pivotPoint);
				graph.center = pivotPoint-diff;
				break;
			case GridPivot.BottomRight:
				pivotPoint = graph.matrix.MultiplyPoint3x4 (new Vector3 (graph.width,0,0));
				diff = pivotPoint-graph.center;
				pivotPoint = EditorGUILayout.Vector3Field ("Bottom-Right",pivotPoint);
				graph.center = pivotPoint-diff;
				break;
		}
		
		graph.GenerateMatrix ();
		
		pivot = PivotPointSelector (pivot);
		
		GUILayout.EndHorizontal ();
		
		GUILayout.BeginHorizontal ();
		graph.rotation = EditorGUILayout.Vector3Field ("Rotation",graph.rotation);
		//Add some space to make the Rotation and postion fields be better aligned (instead of the pivot point selector)
		GUILayout.Space (19+4+7);
		GUILayout.EndHorizontal ();
		
		if (GUILayout.Button (new GUIContent ("Snap Size","Snap the size to exactly fit nodes"),GUILayout.MaxWidth (100),GUILayout.MaxHeight (16))) {
			SnapSizeToNodes (newWidth,newDepth,graph);
		}
		
		Separator ();
		
		graph.cutCorners = EditorGUILayout.Toggle ("Cut Corners",graph.cutCorners);
		graph.neighbours = (NumNeighbours)EditorGUILayout.EnumPopup ("Connections",graph.neighbours);
		
		//GUILayout.BeginHorizontal ();
		//EditorGUILayout.PrefixLabel ("Max Climb");
		graph.maxClimb = EditorGUILayout.FloatField ("Max Climb",graph.maxClimb);
		EditorGUI.indentLevel++;
		graph.maxClimbAxis = EditorGUILayout.IntPopup ("Climb Axis",graph.maxClimbAxis,new string[3] {"X","Y","Z"},new int[3] {0,1,2});
		
		EditorGUI.indentLevel--;
		//GUILayout.EndHorizontal ();
		
		GUILayout.BeginHorizontal ();
		bool preEnabled = GUI.enabled;
		GUI.enabled = graph.useRaycastNormal;
		graph.maxSlope = EditorGUILayout.Slider ("Max Slope",graph.maxSlope,0,90F);
		GUI.enabled = preEnabled;
		graph.useRaycastNormal = GUILayout.Toggle (graph.useRaycastNormal,new GUIContent ("","Use the heigh raycast's normal to figure out the slope of the ground and check if it flat enough to stand on"),GUILayout.Width (10));
		GUILayout.EndHorizontal ();
		
		graph.erodeIterations = EditorGUILayout.IntField ("Erode iterations",graph.erodeIterations);
		graph.erodeIterations = graph.erodeIterations > 16 ? 16 : graph.erodeIterations; //Clamp iterations to 16
		
		DrawCollisionEditor (graph.collision);
		
		Separator ();
		
		showExtra = EditorGUILayout.Foldout (showExtra, "Extra");
		
		if (showExtra) {
			EditorGUI.indentLevel+=2;
			
			graph.penaltyAngle = ToggleGroup ("Angle Penalty",graph.penaltyAngle);
			//bool preGUI = GUI.enabled;
			//GUI.enabled = graph.penaltyAngle && GUI.enabled;
			if (graph.penaltyAngle) {
				EditorGUI.indentLevel++;
				graph.penaltyAngleFactor = EditorGUILayout.FloatField ("Factor",graph.penaltyAngleFactor);
				//GUI.enabled = preGUI;
				HelpBox ("Applies penalty to nodes based on the angle of the hit surface during the Height Testing");
				
				EditorGUI.indentLevel--;
			}
			
			graph.penaltyPosition = ToggleGroup ("Position Penalty",graph.penaltyPosition);
				//EditorGUILayout.Toggle ("Position Penalty",graph.penaltyPosition);
			//preGUI = GUI.enabled;
			//GUI.enabled = graph.penaltyPosition && GUI.enabled;
			if (graph.penaltyPosition) {
				EditorGUI.indentLevel++;
				graph.penaltyPositionOffset = EditorGUILayout.FloatField ("Offset",graph.penaltyPositionOffset);
				graph.penaltyPositionFactor = EditorGUILayout.FloatField ("Factor",graph.penaltyPositionFactor);
				HelpBox ("Applies penalty to nodes based on their Y coordinate\nSampled in Int3 space, i.e it is multiplied with Int3.Precision first (usually 100)");
				//GUI.enabled = preGUI;
				EditorGUI.indentLevel--;
			}
			
			GUI.enabled = false;
			ToggleGroup (new GUIContent ("Use Texture",AstarPathEditor.AstarProTooltip),false);
			GUI.enabled = true;
			EditorGUI.indentLevel-=2;
		}
	}

	
	/** Displays an object field for objects which must be in the 'Resources' folder.
	 * If the selected object is not in the resources folder, a warning message with a Fix button will be shown
	 */
	public UnityEngine.Object ResourcesField (string label, UnityEngine.Object obj, System.Type type) {
		
#if UNITY_3_3
		obj = EditorGUILayout.ObjectField (label,obj,type);
#else
		obj = EditorGUILayout.ObjectField (label,obj,type,false);
#endif
		
		if (obj != null) {
			string path = AssetDatabase.GetAssetPath (obj);
			if (!path.Contains ("Resources/")) {
				if (FixLabel ("Object must be in the 'Resources' folder")) {
					if (!System.IO.Directory.Exists (Application.dataPath+"/Resources")) {
						System.IO.Directory.CreateDirectory (Application.dataPath+"/Resources");
						AssetDatabase.Refresh ();
					}
					
					string error = AssetDatabase.MoveAsset	(path,"Assets/Resources/"+obj.name);
					
					if (error == "") {
						//Debug.Log ("Successful move");
					} else {
						Debug.LogError ("Couldn't move asset - "+error);
					}
				}
			}
		}
		return obj;
	}
	
	
	public void SnapSizeToNodes (int newWidth, int newDepth, GridGraph graph) {
		//Vector2 preSize = graph.unclampedSize;
		
		/*if (locked) {
			graph.unclampedSize = new Vector2 (newWidth*newNodeSize,newDepth*newNodeSize);
			graph.nodeSize = newNodeSize;
			graph.GenerateMatrix ();
			Vector3 newCenter = graph.matrix.MultiplyPoint3x4 (new Vector3 (newWidth/2F,0,newDepth/2F));
			graph.center = newCenter;
			AstarPath.active.AutoScan ();
		} else {*/
			graph.unclampedSize = new Vector2 (newWidth*graph.nodeSize,newDepth*graph.nodeSize);
			Vector3 newCenter = graph.matrix.MultiplyPoint3x4 (new Vector3 (newWidth/2F,0,newDepth/2F));
			graph.center = newCenter;
			graph.GenerateMatrix ();
			AstarPath.active.AutoScan ();
		//}
		
		GUI.changed = true;
	}
	
	public static GridPivot PivotPointSelector (GridPivot pivot) {
		
		GUISkin skin = AstarPathEditor.astarSkin;
		
		GUIStyle background = skin.FindStyle ("GridPivotSelectBackground");
		
		Rect r = GUILayoutUtility.GetRect (19,19,background);
		
		r.width = 19;
		r.height = 19;
		
		if (background == null) {
			return pivot;
		}
		
		if (Event.current.type == EventType.Repaint) {
			background.Draw (r,false,false,false,false);
		}
		
		if (GUI.Toggle (new Rect (r.x,r.y,7,7),pivot == GridPivot.TopLeft, "",skin.FindStyle ("GridPivotSelectButton")))
			pivot = GridPivot.TopLeft;
			
		if (GUI.Toggle (new Rect (r.x+12,r.y,7,7),pivot == GridPivot.TopRight,"",skin.FindStyle ("GridPivotSelectButton")))
			pivot = GridPivot.TopRight;
		
		if (GUI.Toggle (new Rect (r.x+12,r.y+12,7,7),pivot == GridPivot.BottomRight,"",skin.FindStyle ("GridPivotSelectButton")))
			pivot = GridPivot.BottomRight;
			
		if (GUI.Toggle (new Rect (r.x,r.y+12,7,7),pivot == GridPivot.BottomLeft,"",skin.FindStyle ("GridPivotSelectButton")))
			pivot = GridPivot.BottomLeft;	
		
		if (GUI.Toggle (new Rect (r.x+6,r.y+6,7,7),pivot == GridPivot.Center,"",skin.FindStyle ("GridPivotSelectButton")))
			pivot = GridPivot.Center;	
				
		return pivot;
	}
	
	public GridPivot pivot;
	
	public enum GridPivot {
		Center,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}
	
	Matrix4x4 savedMatrix;
	
	Vector3 savedCenter;
	
	public bool isMouseDown = false;
	
	
	Node node1;
	
	//GraphUndo undoState;
	//byte[] savedBytes;
	
	public override void OnSceneGUI (NavGraph target) {
		
		Event e = Event.current;
		
		
		
		GridGraph graph = target as GridGraph;
		
		Matrix4x4 matrixPre = graph.matrix;
		
		graph.GenerateMatrix ();
		
		if (e.type == EventType.MouseDown) {
			isMouseDown = true;
		} else if (e.type == EventType.MouseUp) {
			isMouseDown = false;
		}
		
		if (!isMouseDown) {
			savedMatrix = graph.boundsMatrix;
		}
		
		Handles.matrix = savedMatrix;
		
		if (graph.nodes == null || (graph.uniformWidhtDepthGrid && graph.depth*graph.width != graph.nodes.Length) || graph.matrix != matrixPre) {
			//Rescann the graphs
			AstarPath.active.AutoScan ();
			GUI.changed = true;
		}
		
		Matrix4x4 inversed = savedMatrix.inverse;
		
		Handles.color = AstarColor.BoundsHandles;
		
		Handles.DrawCapFunction cap = Handles.CylinderCap;
		
		Vector2 extents = graph.unclampedSize*0.5F;
		
		//Tools.current is an undocumented editor variable, remove the UseUndocumentedEditorFeatures define at the top of the script to make sure it's forward compatible
		
		/*if (undoState == null) {
			undoState = ScriptableObject.CreateInstance<GraphUndo>();
		}
		
		if (undoState.hasBeenReverted) {
			undoState.ApplyUndo (graph);
			GUI.changed = true;
		}*/
		
		/*if (Event.current.button == 0 && Event.current.isMouse && (/*Event.current.type == EventType.MouseUp || /Event.current.type == EventType.MouseDown)) {
			AstarSerializer sz = new AstarSerializer ();
			sz.OpenSerializeSettings ();
			sz.SerializeSettings (graph,AstarPath.active);
			byte[] bytes = (sz.writerStream.BaseStream as System.IO.MemoryStream).ToArray ();
			sz.Close ();
			
			undoState.data = bytes;
			undoState.hasBeenReverted = true;
			
			Undo.RegisterUndo (undoState,"Graph stuff");
			undoState.hasBeenReverted = false;
			
		}*/
		
		//Undo.SetSnapshotTarget (AstarPath.active.astarData,"Change graph");
		
		Vector3 center = inversed.MultiplyPoint3x4 (graph.center);//Vector3.zero;//inversed.MultiplyPoint3x4 (graph.center);//graph.center;
		
		
#if UNITY_3_3
		if (Tools.current == 3) {
#else
		if (Tools.current == Tool.Scale) {
#endif
		
			Vector3 p1 = Handles.Slider (center+new Vector3 (extents.x,0,0),	Vector3.right,		0.1F*HandleUtility.GetHandleSize (center+new Vector3 (extents.x,0,0)),cap,0);
			Vector3 p2 = Handles.Slider (center+new Vector3 (0,0,extents.y),	Vector3.forward,	0.1F*HandleUtility.GetHandleSize (center+new Vector3 (0,0,extents.y)),cap,0);
			//Vector3 p3 = Handles.Slider (center+new Vector3 (0,extents.y,0),	Vector3.up,			0.1F*HandleUtility.GetHandleSize (center+new Vector3 (0,extents.y,0)),cap,0);
			
			Vector3 p4 = Handles.Slider (center+new Vector3 (-extents.x,0,0),	-Vector3.right,		0.1F*HandleUtility.GetHandleSize (center+new Vector3 (-extents.x,0,0)),cap,0);
			Vector3 p5 = Handles.Slider (center+new Vector3 (0,0,-extents.y),	-Vector3.forward,	0.1F*HandleUtility.GetHandleSize (center+new Vector3 (0,0,-extents.y)),cap,0);
			
			Vector3 p6 = Handles.Slider (center,	Vector3.up,		0.1F*HandleUtility.GetHandleSize (center),cap,0);
			
			Vector3 r1 = new Vector3 (p1.x,p6.y,p2.z);
			Vector3 r2 = new Vector3 (p4.x,p6.y,p5.z);
			
			//Vector3 min = Vector3.Min (r1,r2);
			//Vector3 max = Vector3.Max (r1,r2);
			/*b.Encapsulate (p1);
			b.Encapsulate (p2);
			b.Encapsulate (p3);
			b.Encapsulate (p4);
			b.Encapsulate (p5);
			b.Encapsulate (p6);*/
			
			//Debug.Log (graph.boundsMatrix.MultiplyPoint3x4 (Vector3.zero)+" "+graph.boundsMatrix.MultiplyPoint3x4 (Vector3.one));
			
			//if (Tools.viewTool != ViewTool.Orbit) {
			
				graph.center = savedMatrix.MultiplyPoint3x4 ((r1+r2)/2F);
				
				Vector3 tmp = r1-r2;
				graph.unclampedSize = new Vector2(tmp.x,tmp.z);
				
			//}		
		
#if UNITY_3_3
		} else if (Tools.current == 1) {
#else
		} else if (Tools.current == Tool.Move) {
#endif
			
			if (Tools.pivotRotation == PivotRotation.Local) {	
				center = Handles.PositionHandle (center,Quaternion.identity);
				
				if (Tools.viewTool != ViewTool.Orbit) {
					graph.center = savedMatrix.MultiplyPoint3x4 (center);
				}
			} else {
				Handles.matrix = Matrix4x4.identity;
				
				center = Handles.PositionHandle (graph.center,Quaternion.identity);
				
				if (Tools.viewTool != ViewTool.Orbit) {
					graph.center = center;
				}
			}
#if UNITY_3_3
		} else if (Tools.current == 2) {
#else
		} else if (Tools.current == Tool.Rotate) {
#endif
			//The rotation handle doesn't seem to be able to handle different matrixes of some reason
			Handles.matrix = Matrix4x4.identity;
			
			Quaternion rot = Handles.RotationHandle (Quaternion.Euler (graph.rotation),graph.center);
			
			if (Tools.viewTool != ViewTool.Orbit) {
				graph.rotation = rot.eulerAngles;
			}
		}
		
		//graph.size.x = Mathf.Max (graph.size.x,1);
		//graph.size.y = Mathf.Max (graph.size.y,1);
		//graph.size.z = Mathf.Max (graph.size.z,1);
		
		Handles.matrix = Matrix4x4.identity;
		
		
		
		
		Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
		
		Vector3 p = ray.GetPoint (100);
		
		
		if (Event.current.shift) {
			
			Node close = graph.GetNearest (p);
			
			if (close != null) {
				node1 = close;
			}
		}
		
		
		if (node1 == null) {
			return;
		}
		
		Handles.SphereCap (0,node1.position,Quaternion.identity,graph.nodeSize);
		
		
		Node node = node1;
		
		GUI.color = Color.white;
		Handles.Label((Vector3)node.position + Vector3.up*2,"G : "+node.g+"\nH : "+node.h+"\nF : "+node.f+"\nPosition : "+node.position.ToString (),EditorStyles.whiteBoldLabel);
	}
	
	
	public void SerializeSettings (NavGraph target, AstarSerializer serializer) {
		serializer.AddValue ("pivot",(int)pivot);
		serializer.AddValue ("locked",locked);
		serializer.AddValue ("showExtra",showExtra);
	}
	
	public void DeSerializeSettings (NavGraph target, AstarSerializer serializer) {
		pivot = (GridPivot)serializer.GetValue ("pivot",typeof(int),GridPivot.BottomLeft);
		locked = (bool)serializer.GetValue ("locked",typeof(bool),true);
		showExtra = (bool)serializer.GetValue ("showExtra",typeof(bool));
			
	}
}