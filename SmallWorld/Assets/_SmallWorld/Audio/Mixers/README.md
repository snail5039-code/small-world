# Audio Mixer Contract

Assign a Unity AudioMixer to AudioService and expose these float parameters:

- MasterVolume
- MusicVolume
- SfxVolume
- VoiceVolume

The service remains safe when no mixer is assigned. Mixer asset authoring is performed in Unity so Unity owns all serialized GUID references.

