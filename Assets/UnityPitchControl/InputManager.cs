using UnityEngine;
using System;
using System.Collections.Generic;
using Pitch;

namespace UnityPitchControl.Input {
	public sealed class InputManager : MonoBehaviour {
		public PitchMappings PitchMappings = new PitchMappings();

		public String _audioDevice = ""; // name of the audio device
		public AudioClip _micInput;
		public float[] _samples;
		public PitchTracker _pitchTracker;
		public List<int> _detectedPitches;
		
		private static InputManager _instance;
		private void Awake() {
			_instance = UnityEngine.Object.FindObjectOfType(typeof(InputManager)) as InputManager;
			if (_instance == null) {
				// try to load prefab
				UnityEngine.Object managerPrefab = Resources.Load("InputManager"); // looks inside all 'Resources' folders in 'Assets'
				if (managerPrefab != null) {
					UnityEngine.Object prefab = Instantiate(managerPrefab);
					prefab.name = "InputManager"; // otherwise creates a game object with "(Clone)" appended to the name
				} else if (UnityEngine.Object.FindObjectOfType(typeof(InputManager)) == null) {
					// no prefab found, create new input manager
					GameObject gameObject = new GameObject("InputManager");
					gameObject.AddComponent<InputManager>();
					DontDestroyOnLoad(gameObject);
					gameObject.hideFlags = HideFlags.HideInHierarchy;
				}
				_instance = UnityEngine.Object.FindObjectOfType(typeof(InputManager)) as InputManager;
			}

			// start recording
			int minFreq, maxFreq;
			Microphone.GetDeviceCaps(_audioDevice, out minFreq, out maxFreq);
			if (minFreq > 0) _micInput = Microphone.Start(_audioDevice, true, 1, minFreq);
			else _micInput = Microphone.Start(_audioDevice, true, 1, 44000);

			// prepare for pitch tracking
			_samples = new float[_micInput.samples * _micInput.channels];
			_pitchTracker = new PitchTracker();
			_pitchTracker.SampleRate = _micInput.samples;
			_pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(PitchDetectedListener);
		}
		
		public void Update() {
			_detectedPitches.Clear(); // clear pitches from last update
			_micInput.GetData(_samples, 0);
			_pitchTracker.ProcessBuffer(_samples);

			// update the state of each pitch mapping
			foreach (PitchMapping m in PitchMappings.Mappings) {
				bool conditionMet = false;
				foreach (int pitch in _detectedPitches) {
					if ((pitch > m.minVal) && (pitch <= m.maxVal)) {
						conditionMet =  true;
						break;
					}
				}
				
				m.keyDown = false;
				m.keyUp = false;
				if ((conditionMet) && (!m.conditionMet)) {
					m.keyDown = true; // first time condition is met - KeyDown event
				} else if ((!conditionMet) && (m.conditionMet)) {
					m.keyUp = true; // condition was met last update and now isn't - KeyUp event
				}
				m.conditionMet = conditionMet;
			}
		}

		private void PitchDetectedListener(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) {
			int pitch = (int)Math.Round(pitchRecord.Pitch);
			if (!_detectedPitches.Contains(pitch)) _detectedPitches.Add(pitch);
		}
		
		public bool MapsKey(string key) {
			return PitchMappings.MapsKey(key);
		}
		
		public void MapPitch(int minVal, int maxVal, string key) {
			PitchMappings.MapPitch(minVal, maxVal, key);
		}
		
		public void RemoveMapping(int minVal, int maxVal, string key) {
			PitchMappings.RemoveMapping(minVal, maxVal, key);
		}
		
		public static bool GetKey(string name) {
			if (name == "none") return false;
			
			if ((_instance != null) && _instance.MapsKey(name)) {
				bool mappingTriggered = false;
				foreach (PitchMapping m in _instance.PitchMappings.GetMappings(name)) {
					if (m.conditionMet && !m.keyDown &!m.keyUp) {
						mappingTriggered = true;
						break;
					}
				}
				
				return mappingTriggered || UnityEngine.Input.GetKey(name);
			} else {
				return UnityEngine.Input.GetKey(name);
			}
		}
		
		public static bool GetKey(KeyCode key) {
			return GetKey(key.ToString().ToLower());
		}
		
		public static bool GetKeyDown(string name) {
			if (name == "none") return false;
			
			if ((_instance != null) && _instance.MapsKey(name)) {
				bool mappingTriggered = false;
				foreach (PitchMapping m in _instance.PitchMappings.GetMappings(name)) {
					if (m.keyDown) {
						mappingTriggered = true;
						break;
					}
				}
				
				return mappingTriggered || UnityEngine.Input.GetKeyDown(name);
			} else {
				return UnityEngine.Input.GetKeyDown(name);
			}
		}
		
		public static bool GetKeyDown(KeyCode key) {
			return GetKeyDown(key.ToString().ToLower());
		}
		
		public static bool GetKeyUp(string name) {
			if (name == "none") return false;
			
			if ((_instance != null) && _instance.MapsKey(name)) {
				bool mappingTriggered = false;
				foreach (PitchMapping m in _instance.PitchMappings.GetMappings(name)) {
					if (m.keyUp) {
						mappingTriggered = true;
						break;
					}
				}
				
				return mappingTriggered || UnityEngine.Input.GetKeyUp(name);
			} else {
				return UnityEngine.Input.GetKeyUp(name);
			}
		}
		
		public static bool GetKeyUp(KeyCode key) {
			return GetKeyUp(key.ToString().ToLower());
		}
	}
}