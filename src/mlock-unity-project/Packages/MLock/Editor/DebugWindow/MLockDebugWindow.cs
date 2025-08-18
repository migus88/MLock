using System.Collections.Generic;
using System.Linq;
using Migs.MLock.Debugging;
using UnityEditor;
using UnityEngine;

namespace Migs.MLock.Editor.DebugWindow
{
    /// <summary>
    /// Editor window for debugging MLock system
    /// Follows Single Responsibility principle by only handling UI and user interaction
    /// </summary>
    public class MLockDebugWindow : EditorWindow
    {
        // UI state
        private Vector2 _scrollPosition;
        private bool _isAutoRefresh = true;
        private readonly Dictionary<int, bool> _foldoutStates = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> _originFoldoutStates = new Dictionary<int, bool>();
        private string _searchText = "";
        private enum SearchType { Lockables, Tags, Includes, Excludes }
        private SearchType _searchType = SearchType.Lockables;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _subheaderStyle;
        private GUIStyle _lockItemStyle;
        private GUIStyle _affectedItemStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _categoryStyle;
        
        // Menu item to open the window
        [MenuItem("Window/MLock/Locks Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<MLockDebugWindow>();
            window.titleContent = new GUIContent("MLock Debug");
            window.Show();
        }
        
        // Menu item to open the services window
        [MenuItem("Window/MLock/Services Debug")]
        public static void ShowServicesWindow()
        {
            var window = GetWindow<MLockServicesDebugWindow>();
            window.titleContent = new GUIContent("MLock Services");
            window.Show();
        }
        
        private void OnEnable()
        {
            // Enable debug data collection
            DebugDataHandler.SetEnabled(true);
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            // Disable debug data collection to avoid unnecessary processing
            DebugDataHandler.SetEnabled(false);
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void InitializeStyles()
        {
            if (_headerStyle != null) return;
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(4, 4, 8, 8)
            };
            
            _subheaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(4, 4, 4, 4)
            };
            
            _lockItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(4, 4, 2, 2),
                padding = new RectOffset(8, 8, 8, 8)
            };
            
            _affectedItemStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(20, 4, 1, 1)
            };
            
            _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                margin = new RectOffset(0, 5, 2, 2)
            };
            
            _categoryStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 2, 4),
                padding = new RectOffset(6, 6, 4, 4)
            };
        }
        
        private void OnEditorUpdate()
        {
            if (_isAutoRefresh)
            {
                // Update data and repaint window
                DebugDataHandler.UpdateData();
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            DrawToolbar();
            
            EditorGUILayout.Space();
            
            DrawSearchBar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawActiveLocks();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                DebugDataHandler.UpdateData();
            }
            
            // Auto refresh toggle
            _isAutoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", _isAutoRefresh, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            // Services window button
            if (GUILayout.Button("Services Window", EditorStyles.toolbarButton))
            {
                ShowServicesWindow();
            }
            
            // Unlock all button
            if (GUILayout.Button("Unlock All", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Unlock All", 
                        "Are you sure you want to unlock all active locks?", 
                        "Yes", "Cancel"))
                {
                    DebugDataHandler.UnlockAll();
                    DebugDataHandler.UpdateData();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Search By:", GUILayout.Width(70));
            _searchType = (SearchType)EditorGUILayout.EnumPopup(_searchType, GUILayout.Width(100));
            
            _searchText = EditorGUILayout.TextField(_searchText);
            
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawActiveLocks()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Locks", _headerStyle);
            
            var locks = DebugDataHandler.ActiveLocks;
            if (locks.Count == 0)
            {
                EditorGUILayout.HelpBox("No active locks.", MessageType.Info);
                return;
            }

            // Filter locks based on search
            var filteredLocks = FilterLocks(locks);
            
            if (!filteredLocks.Any() && !string.IsNullOrEmpty(_searchText))
            {
                EditorGUILayout.HelpBox($"No locks matching search: '{_searchText}'", MessageType.Info);
                return;
            }
            
            foreach (var lockInfo in filteredLocks)
            {
                EditorGUILayout.BeginVertical(_lockItemStyle);
                
                // Foldout for lock details
                _foldoutStates.TryAdd(lockInfo.Id, false);

                // Collapsed by default
                EditorGUILayout.BeginHorizontal();
                
                _foldoutStates[lockInfo.Id] = EditorGUILayout.Foldout(_foldoutStates[lockInfo.Id], 
                    $"Lock ID: {lockInfo.Id} - Service: {lockInfo.LockType}");
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Unlock", GUILayout.Width(60)))
                {
                    DebugDataHandler.UnlockById(lockInfo.Id);
                    DebugDataHandler.UpdateData();
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (_foldoutStates[lockInfo.Id])
                {
                    EditorGUI.indentLevel++;
                    
                    DrawLockDetails(lockInfo);
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawLockDetails(LockDebugInfo lockInfo)
        {
            DrawOrigin(lockInfo);

            EditorGUILayout.BeginVertical(_categoryStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Service:", _boldLabelStyle, GUILayout.Width(120));
            GUILayout.Label(lockInfo.LockType);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(_categoryStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Include Tags:", _boldLabelStyle, GUILayout.Width(120));
            GUILayout.Label(lockInfo.IncludeTags ?? "None");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(_categoryStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Exclude Tags:", _boldLabelStyle, GUILayout.Width(120));
            GUILayout.Label(lockInfo.ExcludeTags ?? "None");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Affected Lockables:", _subheaderStyle);
            
            if (lockInfo.AffectedLockables.Count == 0)
            {
                EditorGUILayout.LabelField("No affected lockables", EditorStyles.label);
                return;
            }
            
            foreach (var lockable in lockInfo.AffectedLockables)
            {
                EditorGUILayout.LabelField(lockable, _affectedItemStyle);
            }
        }

        private void DrawOrigin(LockDebugInfo lockInfo)
        {
            EditorGUILayout.BeginVertical(_categoryStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Origin:", _boldLabelStyle, GUILayout.Width(120));
            
            var hasOrigin = lockInfo.OriginFrames is { Count: >= 1 };
            var first = lockInfo.OriginFrames?.FirstOrDefault();

            var originText = hasOrigin ? first!.Display : "Unknown";

            // Expandable stack trace
            var hasFrames = lockInfo.OriginFrames is { Count: > 1 };
            if (hasFrames)
            {
                _originFoldoutStates.TryAdd(lockInfo.Id, false);

                if (EditorGUILayout.LinkButton(originText))
                {
                    _originFoldoutStates[lockInfo.Id] = !_originFoldoutStates[lockInfo.Id];
                }
            }
            else
            {
                GUILayout.Label(originText);
            }
            
            EditorGUILayout.EndHorizontal();

            DrawStackTrace(lockInfo, hasFrames);
            EditorGUILayout.EndVertical();
        }

        private void DrawStackTrace(LockDebugInfo lockInfo, bool hasFrames)
        {
            if (!hasFrames || !_originFoldoutStates[lockInfo.Id])
            {
                return;
            }
            
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(10);
            foreach (var frame in lockInfo.OriginFrames)
            {
                if (string.IsNullOrEmpty(frame.File) || frame.Line is not > 0)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                
                GUILayout.Label("> ", GUILayout.Width(10));
                if (EditorGUILayout.LinkButton(frame.Display))
                {
                    OpenScriptAtLine(frame.File, frame.Line.Value);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
            }
            EditorGUI.indentLevel--;
        }

        private List<LockDebugInfo> FilterLocks(IEnumerable<LockDebugInfo> locks)
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                if (locks is List<LockDebugInfo> list)
                {
                    return list;
                }

                return locks.ToList();
            }
            
            return locks.Where(lockInfo =>
            {
                switch (_searchType)
                {
                    case SearchType.Tags:
                        return lockInfo.LockType.ToLower().Contains(_searchText.ToLower());
                    case SearchType.Includes:
                        return lockInfo.IncludeTags.ToLower().Contains(_searchText.ToLower());
                    case SearchType.Excludes:
                        return lockInfo.ExcludeTags.ToLower().Contains(_searchText.ToLower());
                    case SearchType.Lockables:
                    default:
                        return lockInfo.AffectedLockables.Any(l => l.ToLower().Contains(_searchText.ToLower()));
                }
            }).ToList();
        }

        private static void OpenScriptAtLine(string filePath, int line)
        {
            if (string.IsNullOrEmpty(filePath) || line <= 0)
            {
                return;
            }

            // Use Unity internal utility to open external script editor at specific line
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, line);
        }
    }
}