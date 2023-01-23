using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

// A behaviour that is attached to a playable
public class ConversationPlayable : PlayableBehaviour
{
	private GameObject _canvasObject;
	private Image _dialogueBoxDisplay;
	private TMP_Text _dialogTextDisplay;
	private TMP_FontAsset _fontAsset;
	private Color _color;
	private string _textString;
	private Sprite _npcHead;

	public void Initialize(GameObject canvasObject, Image dialogueBoxDisplay, TMP_Text dialogTextDisplay, TMP_FontAsset fontAsset, Sprite npcHead, Color color, string textString)
	{
		_canvasObject = canvasObject;
		_dialogueBoxDisplay = dialogueBoxDisplay;
		_dialogTextDisplay = dialogTextDisplay;
		_fontAsset = fontAsset;
		_color = color;
		_textString = textString;
		_npcHead = npcHead;
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info) 
	{
		_canvasObject.SetActive (true);
		_dialogTextDisplay.font = _fontAsset;
		_dialogTextDisplay.color = _color;
		_dialogTextDisplay.text = _textString;
		_dialogueBoxDisplay.sprite = _npcHead;
	}

	public override void OnBehaviourPause (Playable playable, FrameData info)
	{
		_canvasObject.SetActive (false);
	}
}
