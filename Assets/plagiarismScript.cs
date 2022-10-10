using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class plagiarismScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public SpriteRenderer[] Sprites;
    public Sprite[] Shapes;
    public Sprite[] ColorblindShapes;
    public KMSelectable Pass;
    public KMSelectable Report;
    public KMColorblindMode Colorblind;
    public SpriteRenderer Back;
    public Sprite[] Papers;
    public Sprite Empty;

    // % 0 = hexagon, 1 = triangle, 2 = circle, 3 = square, 4 = heart
    // / 0 = purple, 1 = blue, 2 = orange, 3 = green, 4 = red

    int leftmostDigit, secondLeftmostDigit;
    int[] sourceShapes = { 1, 2, 2, 2, 0, 3, 2, 0, 1, 3, 3, 4, 1, 0, 3, 2, 4, 1, 2, 3, 4, 0, 1, 1, 3, 1, 4, 3, 0, 0, 2, 2, 0, 1, 1, 3, 0, 0, 4, 1, 1, 4, 2, 4, 4, 2, 3, 4, 0, 2, 0, 0, 3, 2, 3, 1, 4, 4, 3, 4 };
    int[] sourceColors = { 2, 1, 1, 4, 2, 0, 0, 3, 4, 0, 0, 2, 3, 0, 2, 3, 3, 1, 2, 0, 4, 1, 1, 4, 1, 3, 2, 4, 4, 1, 1, 3, 1, 2, 4, 4, 3, 4, 0, 3, 0, 1, 2, 1, 4, 0, 0, 3, 2, 0, 3, 2, 4, 0, 3, 3, 2, 4, 2, 1 };
    bool colorblindActive = false;
    string[] shapeNames = { "hexagon", "triangle", "circle", "square", "heart" };
    string[] colorNames = { "purple", "blue", "orange", "green", "red" };
    int[] finalSequence = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    int stage = 1;
    bool correctAnswer;
    int[] shownSequence = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    string[] coloredShapeNames = { "px", "pt", "pc", "ps", "ph", "bx", "bt", "bc", "bs", "bh", "ox", "ot", "oc", "os", "oh", "gx", "gt", "gc", "gs", "gh", "rx", "rt", "rc", "rs", "rh" };
    string[] finalSeqNames = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
    string[] shownSeqNames = { "", "", "", "", "", "", "", "", "", "" };

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;

        Pass.OnInteract += delegate () { ButtonPress(true); return false; };
        Report.OnInteract += delegate () { ButtonPress(false); return false; };
    }

    // Use this for initialization
    void Start () {
        if (Colorblind.ColorblindModeActive) {
            colorblindActive = true;
        }
        Debug.LogFormat("<Plagiarism #{0}> Colorblind mode: {1}", moduleId, colorblindActive);

        leftmostDigit = Bomb.GetSerialNumberNumbers().ToArray()[0];
        sourceShapes = sourceShapes.Where(s => s != leftmostDigit%5).ToArray();
        Debug.LogFormat("[Plagiarism #{0}] Leftmost digit of the serial number is {1}, removing all instances of {2}s from the source.", moduleId, leftmostDigit, shapeNames[leftmostDigit%5]);

        secondLeftmostDigit = Bomb.GetSerialNumberNumbers().ToArray()[1];
        sourceColors = sourceColors.Where(s => s != secondLeftmostDigit%5).ToArray();
        Debug.LogFormat("[Plagiarism #{0}] Second-leftmost digit of the serial number is {1}, removing all instances of {2}s from the source.", moduleId, secondLeftmostDigit, colorNames[secondLeftmostDigit%5]);

        for (int e = 0; e < 48; e++) {
            finalSequence[e] = (sourceColors[e] * 5) + sourceShapes[e];
            finalSeqNames[e] = coloredShapeNames[finalSequence[e]];
        }
        Debug.LogFormat("[Plagiarism #{0}] Final sequence: {1}", moduleId, finalSeqNames.Join(", "));

        GenerateStage();
    }

    void GenerateStage () {
        int RNG = UnityEngine.Random.Range(0, 2);
        correctAnswer = RNG == 0;
        if (correctAnswer) { //Pass
            
            regenSeq:
            bool acceptable = true;
            int matches = 0;
            int insertedStuff = 0;

            for (int b = 0; b < 10; b++) {
                regenShape:
                shownSequence[b] = UnityEngine.Random.Range(0, 25);
                if (shownSequence[b] % 5 == leftmostDigit % 5 || shownSequence[b] / 5 == secondLeftmostDigit % 5) {
                    goto regenShape;
                }
            }
            Debug.Log("<Plagiarism> Original shown sequence: " + shownSequence.Join(", "));

            for (int i = 0; i < 10; i++) {
                RNG = UnityEngine.Random.Range(0, 25);
                if (RNG < 9) {
                    insertedStuff++;
                    shownSequence = InsertBanned(i, RNG);
                }
            }

            int y = 0;
            int[] h = {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
            for (int v = 0; v < 10; v++) {
                if (shownSequence[v] % 5 == leftmostDigit % 5 || shownSequence[v] / 5 == secondLeftmostDigit % 5) {
                    continue;
                } else {
                    h[y] = shownSequence[v];
                    y++;
                }
            }

            if (h[0] == -1) {
                acceptable = false;
            }

            for (int k = 0; k < 39; k++) {
                if (h[0] == finalSequence[k]) {
                    for (int n = 1; n < 10-y; n++) {
                        if (h[n] == finalSequence[k+n]) {
                            matches++;
                        }
                    }
                    if (matches == 9-y) {
                        acceptable = false;
                    }
                }
            }      

            if (!acceptable) {
                Debug.Log("<Plagiarism> Unacceptable sequence found!");
                goto regenSeq;
            }

        } else { //Report
            RNG = UnityEngine.Random.Range(0, 39);
            for (int c = 0; c < 10; c++) {
                shownSequence[c] = finalSequence[c+RNG];
            }
            Debug.Log("<Plagiarism> Original shown sequence: " + shownSequence.Join(", "));

            for (int i = 0; i < 10; i++) {
                RNG = UnityEngine.Random.Range(0, 25);
                if (RNG < 9) {
                    shownSequence = InsertBanned(i, RNG);
                }
            }
        }

        for (int s = 0; s < 10; s++) {
            if (colorblindActive) {
                Sprites[s].sprite = ColorblindShapes[shownSequence[s]];
            } else {
                Sprites[s].sprite = Shapes[shownSequence[s]];
            }
            shownSeqNames[s] = coloredShapeNames[shownSequence[s]];
        }
        Debug.LogFormat("[Plagiarism #{0}] Shown sequence for stage {1}: {2}; answer is {3}.", moduleId, stage, shownSeqNames.Join(", "), correctAnswer ? "Pass" : "Report");
    }

    int[] InsertBanned(int q, int r) {
        int s = Enumerable.Range(0, 5).Where(x => x != (leftmostDigit % 5)).PickRandom();
        int c = Enumerable.Range(0, 5).Where(x => x != (secondLeftmostDigit % 5)).PickRandom();
        bool d = UnityEngine.Random.Range(0, 2) == 0;

        int[] a = shownSequence;

        if (d) {
            for (int w = 9; w > -1; w--) {
                if (w > q) {
                    a[w] = shownSequence[w-1];
                } else if (w == q) {
                    if (r == 0) {
                        a[w] = (secondLeftmostDigit % 5) * 5 + (leftmostDigit % 5);
                    } else if (r < 5) {
                        a[w] = (secondLeftmostDigit % 5) * 5 + s;
                    } else {
                        a[w] = c * 5 + (leftmostDigit % 5);
                    }
                    break;
                } else {
                    a[w] = shownSequence[w];
                }
            }
        } else {
            for (int w = 0; w < 10; w++) {
                if (w < q) {
                    a[w] = shownSequence[w+1];
                } else if (w == q) {
                    if (r == 0) {
                        a[w] = (secondLeftmostDigit % 5) * 5 + (leftmostDigit % 5);
                    } else if (r < 5) {
                        a[w] = (secondLeftmostDigit % 5) * 5 + s;
                    } else {
                        a[w] = c * 5 + (leftmostDigit % 5);
                    }
                    break;
                } else {
                    a[w] = shownSequence[w];
                }
            }
        }
        
        Debug.Log("<Plagiarism> Inserting in position " + q + " with random number " + r + " and shifting " + (d ? "forward" : "backward") + " gives " + a.Join(", "));
        return a;
    }

    void ButtonPress(bool p) {
        if (p) {
            Pass.AddInteractionPunch();
        } else {
            Report.AddInteractionPunch();
        }
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        if (moduleSolved) {
            return;
        }

        if (p == correctAnswer) {
            Debug.LogFormat("[Plagiarism #{0}] Pressed {1}, answer is {1}. That is correct.", moduleId, p ? "Pass" : "Report");
        } else {
            Debug.LogFormat("[Plagiarism #{0}] Pressed {1}, answer is {2}. That is incorrect. Strike!", moduleId, p ? "Pass" : "Report", p ? "Report" : "Pass");
            GetComponent<KMBombModule>().HandleStrike();
        }
        Back.sprite = Papers[stage];
        stage++;
        if (stage > 4) {
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[Plagiarism #{0}] All papers graded, module solved.", moduleId);
            moduleSolved = true;
            for (int z = 0; z < 10; z++) {
                Sprites[z].sprite = Empty;
            }
        } else {
            GenerateStage();
        }
    }
 
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To press the report/pass button, use !{0} report/pass";
    #pragma warning restore 414
    
    IEnumerator ProcessTwitchCommand(string command)
    {
		if (Regex.IsMatch(command, @"^\s*report\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			Report.OnInteract();
            yield break;
        }
		
		if (Regex.IsMatch(command, @"^\s*pass\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			Pass.OnInteract();
            yield break;
        }
	}

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            (correctAnswer ? Pass : Report).OnInteract();
            if (!moduleSolved)
                yield return new WaitForSeconds(0.2f);
        }
    }
}
