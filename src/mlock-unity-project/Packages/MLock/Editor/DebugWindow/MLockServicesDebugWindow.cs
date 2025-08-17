using System.Collections.Generic;
using System.Linq;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Migs.MLock.Editor.DebugWindow
{
    /// <summary>
    /// Editor window for displaying MLock services
    /// </summary>
    public class MLockServicesDebugWindow : EditorWindow
    {
        // UI state
        private Vector2 _scrollPosition;
        private bool _isAutoRefresh = true;
        private readonly Dictionary<string, bool> _foldoutStates = new();
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _subheaderStyle;
        private GUIStyle _serviceItemStyle;
        
        private void OnEnable()
        {
            // Enable debug data collection
            DebugDataHandler.SetEnabled(true);
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
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
            
            _serviceItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(4, 4, 2, 2),
                padding = new RectOffset(8, 8, 8, 8)
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
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawLockServices();
            
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
            
            // Lock window button
            if (GUILayout.Button("Locks Window", EditorStyles.toolbarButton))
            {
                MLockDebugWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLockServices()
        {
            EditorGUILayout.LabelField("Lock Services", _headerStyle);
            
            var services = DebugDataHandler.LockServices;
            if (services.Count == 0)
            {
                EditorGUILayout.HelpBox("No lock services registered. Lock services must be registered with MLockDebugData.RegisterLockService().", MessageType.Info);
                return;
            }
            
            foreach (var service in services)
            {
                var type = service.GetType();
                var serviceName = type.Name;
                var foldoutKey = type.FullName ?? serviceName;

                // Determine tag type(s) from implemented ILockService<T>
                var tagTypeNames = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILockService<>))
                    .Select(i => i.GetGenericArguments().First().Name)
                    .Distinct()
                    .ToList();
                
                var tagType = tagTypeNames.Count > 0 ? string.Join(", ", tagTypeNames) : "Unknown";
                
                _foldoutStates.TryAdd(foldoutKey, true);
                
                EditorGUILayout.BeginVertical(_serviceItemStyle);
                
                _foldoutStates[foldoutKey] = EditorGUILayout.Foldout(_foldoutStates[foldoutKey], 
                    $"Lock Service: {serviceName}");
                
                if (_foldoutStates[foldoutKey])
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField($"Tag Type: {tagType}", _subheaderStyle);
                    EditorGUILayout.LabelField($"Service Implementation: {type.FullName}");
                    
                    // Count active locks for this service by matching tag types
                    var lockCount = DebugDataHandler.ActiveLocks.Count(lockInfo => tagTypeNames.Contains(lockInfo.LockType));

                    EditorGUILayout.LabelField($"Active Locks: {lockCount}");
                    
                    if (GUILayout.Button("View Locks"))
                    {
                        // Open the locks window
                        MLockDebugWindow.ShowWindow();
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
    }
} 