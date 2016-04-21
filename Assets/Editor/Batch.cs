using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BatchLightmapBaker : EditorWindow
{
	int _numScenes;
	List<bool> _shouldBakeScene;
	Vector2 _scrollPosition;

	List<string> _scenePathsToBake;
	int _currentlyBakingScene;
	bool _bakeInProgress;
	bool _bakeWasAlreadyInProgress;

	AsyncOperation _sceneBakeCoroutine;

	[MenuItem( "Tools/Batch lightmap baker..." )]
	static void Init()
	{
		// Get existing open window or if none, make a new one:
		var window = GetWindow<BatchLightmapBaker>();
		window.titleContent.text = "Batch Lightmap Baker";
		window.Show();

		window._numScenes = UnityEditor.SceneManagement.EditorSceneManager.sceneCountInBuildSettings;
		window._shouldBakeScene = new List<bool>( window._numScenes );
		for( int sceneIndex = 0; sceneIndex < window._numScenes; ++sceneIndex )
		{
			window._shouldBakeScene.Add( EditorBuildSettings.scenes[ sceneIndex ].enabled ); // todo: remember this from last time.
		}

		window._scrollPosition = Vector2.zero;

		window._scenePathsToBake = new List<string>();
		window._currentlyBakingScene = 0;
		window._bakeInProgress = false;
		window._bakeWasAlreadyInProgress = Lightmapping.isRunning;
	}

	void OnGUI()
	{
		if( _bakeWasAlreadyInProgress )
		{
			GUILayout.Label( "Bake was in progress before opening window. Waiting for bake to finish." );
			if( !Lightmapping.isRunning )
			{
				_bakeWasAlreadyInProgress = false;
			}
			return;
		}

		if( _bakeInProgress )
		{
			GUILayout.Label( "Baking in progress" );

			float progress = (float)_currentlyBakingScene / _scenePathsToBake.Count + ( 1.0f / _scenePathsToBake.Count ) * Lightmapping.buildProgress;

			var shouldCancel = EditorUtility.DisplayCancelableProgressBar( "Lightmap bake", "Baking " + _currentlyBakingScene + "/" + _scenePathsToBake.Count, progress );

			// todo: fix this. It doesn't seem to work, though it's exactly how the Unity docs say to use it. Maybe the scene loading/saving popping up interrupts the current progress bar?
			if( shouldCancel )
			{
				Debug.Log( "Cancelling bake" );
				Lightmapping.Cancel();
				_bakeInProgress = false;
			}
		}
		else
		{
			if( GUILayout.Button( "All" ) )
			{
				for( int sceneIndex = 0; sceneIndex < _numScenes; ++sceneIndex )
				{
					_shouldBakeScene[ sceneIndex ] = true;
				}
			}

			if( GUILayout.Button( "None" ) )
			{
				for( int sceneIndex = 0; sceneIndex < _numScenes; ++sceneIndex )
				{
					_shouldBakeScene[ sceneIndex ] = false;
				}
			}

			int numSelected = 0;

			if( _numScenes > 0 )
			{
				_scrollPosition = GUILayout.BeginScrollView( _scrollPosition );


				for( int sceneIndex = 0; sceneIndex < _numScenes; ++sceneIndex )
				{
					_shouldBakeScene[ sceneIndex ] = EditorGUILayout.ToggleLeft( EditorBuildSettings.scenes[ sceneIndex ].path, _shouldBakeScene[ sceneIndex ] );
					if( _shouldBakeScene[ sceneIndex ] )
					{
						++numSelected;
					}
				}

				GUILayout.EndScrollView();
			}

			// Grey out the bake button if no scenes are selected.
			GUI.enabled = numSelected > 0;

			if( GUILayout.Button( "Bake selected (" + numSelected + ")" ) && numSelected > 0 )
			{
				BakeSelected();
			}

			// Clear greyed out controls.
			GUI.enabled = true;
		}
	}

	void BakeSelected()
	{
		_bakeInProgress = true;
		_scenePathsToBake.Clear();

		for( int sceneIndex = 0; sceneIndex < _numScenes; ++sceneIndex )
		{
			if( _shouldBakeScene[ sceneIndex ] )
			{
				_scenePathsToBake.Add( EditorBuildSettings.scenes[ sceneIndex ].path );
			}
		}

		// Bake scene '-1' and don't actually start a bake, so the next update moves onto the first scene. This saves duplicating the logic here and in the update.
		_currentlyBakingScene = -1;
	}

	void Update()
	{
		if( !_bakeInProgress )
		{
			return;
		}

		if( Lightmapping.isRunning )
		{
			// Let it run...
			return;
		}

		// There's no bake happening and there should be!

		// If we were baking an actual scene, save it.
		if( _currentlyBakingScene != -1 )
		{
			Debug.Log( "Lightmap bake completed" );
			Debug.Log( "Saving scene so lighting data gets saved" );

			UnityEditor.SceneManagement.EditorSceneManager.SaveScene( UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene() );
		}

		// Move onto the next one.
		++_currentlyBakingScene;

		// If there aren't any more scenes to bake, finish.
		if( _currentlyBakingScene >= _scenePathsToBake.Count )
		{
			_bakeInProgress = false;
			Debug.Log( "All scenes baked! Finished." );
			EditorUtility.ClearProgressBar();
			return;
		}

		Debug.Log( "Loading scene " + _scenePathsToBake[ _currentlyBakingScene ] );

		// Load the next scene.
		// We do it synchronously because using coroutines would be a huge pain in an editor script...
		var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene( _scenePathsToBake[ _currentlyBakingScene ] );

		// It's possible the load failed, so log a message.
		if( scene.isLoaded )
		{
			Debug.Log( "Scene loaded successfully" );
		}
		else
		{
			Debug.Log( "Scene failed to load. Skipping." );
			return;
		}

		Debug.Log( "Starting bake" );

		// Start the lightmap bake
		var bakeStartResult = Lightmapping.BakeAsync();

		// It's possible for the bake to not start, so log a message.
		if( bakeStartResult )
		{
			Debug.Log( "Bake started successfully" );
		}
		else
		{
			Debug.Log( "Lightmapping.BakeAsync failed to start bake. Skipping this scene." );
			return;
		}
	}
}
