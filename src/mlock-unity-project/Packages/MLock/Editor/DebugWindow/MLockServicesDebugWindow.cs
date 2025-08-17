using System.Collections.Generic;
using System.Linq;
using Migs.MLock.Debugging;
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
                
                _foldoutStates.TryAdd(serviceName, true);
                
                EditorGUILayout.BeginVertical(_serviceItemStyle);
                
                _foldoutStates[serviceName] = EditorGUILayout.Foldout(_foldoutStates[serviceName], 
                    $"Lock Service: {serviceName}");
                
                if (_foldoutStates[serviceName])
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField($"Tag Type: {serviceName}", _subheaderStyle);
                    EditorGUILayout.LabelField($"Service Implementation: {type.FullName}");
                    
                    // Count active locks for this service
                    var lockCount = DebugDataHandler.ActiveLocks.Count(lockInfo => lockInfo.LockType == serviceName);

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