using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndView : View
{
    [SerializeField] private TMP_Text _gameResultText = null;
    public TMP_Text gameResultText => _gameResultText;
}
