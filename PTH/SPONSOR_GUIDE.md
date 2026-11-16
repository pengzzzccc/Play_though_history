# Unknown Technology — Sponsor Guide

**A plain-language guide to playing, keeping the project and reporting problems**  
Prepared on **16 November 2026**, from the current project files.

You do not need to understand programming to use this guide. The companion [Developer Handover](DEVELOPER_HANDOVER.md) gives a future developer the technical detail needed to maintain the game.

This guide describes what is present in the project. A new playable build and a full playtest were not carried out while writing it; the team should check the delivered version on your computer before signing off the handover.

## 1. What the project is

**Unknown Technology** is a museum exploration and puzzle game. You see the museum through the character's eyes, learn about historical technology and use a special sceptre to pick up and place objects. The main activity currently focuses on papyrus in the Ancient exhibition: a short introduction, a narrated story, a restoration puzzle and a quiz.

The project folder is called **PTH**, so you may see that name in files or error messages.

| Included now | Still unfinished or limited |
|---|---|
| Main menu, settings and three save slots. | Modern and Future exhibition scenes are placeholders, with a locked exhibit. |
| Movement, object carrying and placement. | Controller-only play is incomplete; some steps require the keyboard. |
| Ancient papyrus story, wall puzzle and quiz. | One quiz question is still placeholder text. |
| Saving the player's location and some room objects. | Saving does not yet remember the entire puzzle, introduction or quiz progress. |

These unfinished areas should be discussed as future development work. A locked placeholder exhibition does not, by itself, mean the installation has broken.

## 2. What you should receive at handover

Ask the team to supply and identify:

1. **A playable game folder:** the version you open to play or demonstrate. On Windows it contains an application ending in `.exe` plus supporting files and folders.
2. **The source repository:** the working project a developer opens in Unity to change the game. A repository is a stored copy of the project with a history of changes.
3. **These two guides:** this Sponsor Guide and the Developer Handover.
4. **A delivery record:** the build date/version, which computer it was tested on, the accepted unfinished items and who to contact for help.

Keep the whole playable folder together. Sending only the `.exe` file may leave out files the game needs. Keep an untouched backup of the delivered game and source project, ideally in a second location.

The source project and the playable game serve different purposes. If you only want to play, use the delivered game application. If there is no playable application, ask a developer to build one from the source. A normal player does not need to install Unity.

No support person or response time is specified in these guides. Agree a maintenance contact and an email address or issue-reporting location with the team as part of handover.

## 3. Start and close the game

These instructions assume the team supplies a **Windows desktop version**. Other versions need their own tested launch instructions.

1. If supplied as a ZIP, extract the **whole folder** before opening the game.
2. Put it somewhere your account can save files, such as a folder inside Documents. The game keeps its settings and saved games beside the application, so it needs permission to write there.
3. Open the supplied game application. Select **Play**.
4. Choose one of the three save slots. An **Empty** slot starts a new game; a slot showing a saved exhibition/time continues that game.
5. To finish playing, press **Esc**, then choose **Save & Quit**. This returns to the main menu. Choose **Quit** there to close the application.

Use the same slot to continue the same game. For a fresh demonstration, use a different empty slot so you can keep an existing save.

**Save & Quit is the intended way to keep your latest progress.** Closing the window, forcing the game to stop or a computer crash can lose changes since the last save. Even when the player position is saved, the current version may require you to repeat the story/puzzle steps described below.

## 4. Controls

A keyboard and mouse are recommended for the current version because some interactions require **T**.

| What you want to do | Keyboard / mouse |
|---|---|
| Walk | W, A, S, D, or the arrow keys. |
| Look around | Move the mouse. |
| Jump | Space. |
| Talk to the character, examine the artefact or start the wall puzzle | Move close enough for the prompt, then press **T**. |
| Pick up and carry an object with the sceptre | Aim at it and **hold F or the right mouse button**. |
| Place an object | While carrying, aim at the intended blank/slot and release the held button. Without a suitable target, the object drops. |
| Check the wall puzzle | Fill all blanks, aim the centre of the screen at **Check**, then click the **left mouse button**. |
| Pause / resume | Esc opens pause; choose Resume or press Esc again. |
| Choose menu or quiz options | Click with the mouse; menu navigation also supports arrows and Enter. |
| Close dialogue/quiz or skip the flashback | Esc, or the visible on-screen controls. |

The game normally captures the mouse while you walk, so the pointer disappears and mouse movement turns the view. This is expected. Pause to use the menu pointer.

A controller can move/look with its sticks, jump with A/Cross, use the sceptre with the right trigger and pause with Start. Menus support controller navigation. Keep a keyboard available for the T interactions; the current version cannot be described as fully playable with a controller alone.

## 5. A walkthrough of the Ancient activity

The game currently uses both **Jimmy** and **Timmy** for the same character in different prompts. This naming inconsistency is known; follow the nearby interaction prompt.

1. **Speak to the character.** Approach and press T when prompted. Use Next and Exit to finish the introduction.
2. **Examine the papyrus artefact.** Approach it and press T. A five-part flashback tells the story through narration and images.
3. **Move through the story.** Select Next for each part and Finish at the end. There is also a Skip flashback option. Finishing or skipping unlocks the wall activity in the current version.
4. **Start the wall puzzle.** Approach the papyrus wall display and press T when prompted. The display turns on.
5. **Fill the four blanks.** Aim at an answer card, hold F or right mouse to carry it, aim at a blank and release. Try a different card if you want to change an answer. A highlighted target shows where a card can go; it does not prove the answer is correct.
6. **Check the answers.** Fill every blank, put down the held card, aim at Check and left-click. Wait for the result animation. Green means correct; red means incorrect. Change incorrect answers and check again.
7. **If three checks fail, revisit the artefact.** The screen asks you to watch the narration again. Finish or skip that replay to get a fresh attempt, then restart the wall puzzle.
8. **Return to the character after restoring the papyrus.** Press T to take the quiz. Click an answer for each question. Three wrong answers across an attempt end that attempt; talk to the character again to retry. A successful attempt shows a completion screen.

The quiz currently includes a placeholder question saying **“The answer is C”** with four choices all labelled C. The third option is the one currently accepted. This should be replaced by the development team before the quiz is treated as finished educational content.

Objects can drop if a wall blocks the sceptre's connection. Loose wall-puzzle cards can also return to their starting positions. These are intended behaviours, although an object becoming permanently unreachable should be reported.

## 6. Make the game more comfortable

Open **Settings** from the main menu or pause menu. Changes are saved as you adjust them.

| Settings tab | What it changes |
|---|---|
| Control | Mouse/controller sensitivity and Invert Y, which reverses looking up/down. Lower sensitivity if looking around feels too fast. |
| Graphic | Quality and fullscreen. If the game feels slow, try another quality option. |
| Audio | Overall volume, music, sound effects (SFX) and interface sounds (UI). |
| General | Interface size and Reduced Motion. Reduced Motion reduces some movement effects. |

Interface size mainly affects the menus and screen interface; it may not enlarge every quiz, wall label or prompt. Report any text that is too small or cut off.

The current pause menu stops player control, but some objects or animations can keep moving. It is best to place an object down before pausing or saving.

## 7. Saved games, backups and updates

On the Windows version, look in the **same folder as the game application**:

| Folder/file | In everyday terms |
|---|---|
| `Saves` | Your three saved-game files: `slot_1.json`, `slot_2.json`, `slot_3.json`. |
| `Data/settings.json` | Your chosen volume, controls and display preferences. |

You do not need to open or edit these files. To make a backup, close the game and **copy the entire `Saves` folder and the `Data` folder** to a separate, dated backup folder. Keep that copy outside the game folder so it survives replacement of the game.

Saved games are local to that copy of the game. Moving the game to another computer or replacing it with a new version does not automatically transfer those saves. Give the backup to the developer and ask whether old saves are compatible before installing an update or copying them into a new version.

**Current saving limitation:** the game records location and some object positions, but does not record the whole papyrus activity or quiz. After continuing, the wall may be locked again and the introduction, narration or puzzle may need to be repeated. If the activity becomes impossible to continue, keep the save and report it; use a different empty slot for a fresh attempt.

The Delete control on the save screen asks for a second confirming press within three seconds. Confirming removes that slot's normal save file. There is no in-game undo, so make a backup first if you might need it.

If a save cannot be understood, the game may rename it to a file such as `slot_1.corrupt.json`, and the slot may look empty. Keep that file for the developer. It is not a guaranteed recoverable backup. If a previously occupied slot unexpectedly looks empty, copy `Saves` before starting again in that slot.

## 8. If something goes wrong

First note what you were doing and capture the message or screen. If possible, close the game normally and try opening the **same delivered version** once more. Save & Quit is useful when leaving normally, but if the problem concerns missing/broken progress, preserve the existing save files before making more attempts.

| What you notice | What you can try | When to ask the developer |
|---|---|---|
| The game will not open. | Check the ZIP was fully extracted and the application is still with all its supporting files. Try the original delivered copy. | It still will not start, closes immediately or shows an error. Send the exact message. |
| The game says services are unavailable or menu buttons stay disabled. | Restart once. | This generally needs a project/startup check by a developer. |
| Mouse pointer disappears while walking. | Press Esc to pause. | The pointer stays missing in the menu or controls do not return after Resume. |
| You cannot walk or look around. | Close the dialogue/quiz/flashback, choose Resume and click the game window. | Controls remain stuck. Mention which screen was open. |
| T does nothing. | Move closer until the prompt appears. Check that a menu or story screen is not open. Use the keyboard even if using a controller. | A visible prompt repeatedly fails. |
| The papyrus screen is off or will not start. | Finish/skip the artefact story, then approach the wall and press T. After three failures, replay the story. | These steps do not unlock it, especially after Continue. |
| Check does nothing. | Fill all four blanks, release the card, aim at Check and left-click; wait for any existing feedback animation. | All requirements appear satisfied and it still does nothing. |
| The quiz will not start. | Finish the introduction and complete the wall puzzle; wait for the result, then return to the character. | It remains unavailable after completion. |
| A controller disconnects. | Reconnect it or use keyboard/mouse, then choose Resume. | Controls cannot be restored. |
| There is no sound. | Check the computer output/speakers and the game's Master/Music/SFX/UI volumes. | Some sounds stay silent or ignore the controls. Mention which ones. |
| The picture is slow or text is cut off. | Try another quality level, window/fullscreen mode or smaller interface size. | It persists. Include the screen resolution and a screenshot. |
| Progress is missing after reopening. | Check you selected the same slot and same game folder. Remember the puzzle/quiz saving limitation above. Back up the files before more attempts. | Location or generic objects are also lost, or the activity becomes stuck. |
| Modern/Future is locked or empty. | Check whether that area was included in the agreed delivery scope. | The delivered build was supposed to contain a completed area. |
| Pink surfaces, a lasting black/loading screen, a crash or an unreachable object. | Capture it and restart once after preserving any useful files. | Send a report if it repeats or prevents progress. |

If the problem seems limited to settings, a developer may ask you to close the game and **rename** `Data/settings.json` to `settings-backup.json`, then reopen. This resets preferences without deliberately deleting saved games. Keep the renamed file until the issue is resolved. Avoid editing project files or deleting folders to try to repair gameplay.

An error may appear in a log rather than on screen. The current game does not have an automatic error-reporting dashboard, and it may return to the menu even if a save failed. If saving is in doubt, check the slot's displayed save time after returning to Play and ask the developer to confirm it actually saved.

## 9. How to report a problem

Send this to the agreed maintenance contact, or add it to the repository's Issues area if the team has enabled that and given you access. A useful report is:

```text
Problem title:
Game version/build date (or the name of the supplied game folder):
Date and time it happened:
Computer and Windows version:
Keyboard/mouse or controller:
Save slot and whether this was New Game or Continue:
Where I was in the game:
Steps I took, starting from opening the game:
What I expected:
What actually happened:
Exact message, if any:
Does it happen again? Does it stop me continuing?
Attachments: screenshot/video, log, and a copied save if relevant.
```

For example: “After Continue in Slot 2, I approached the wall and pressed T. I expected the puzzle to start, but there was no prompt. It happened twice. I can walk around, but cannot continue the activity.”


## 10. Keeping the project usable in the future

Keep control of the repository account/access, the original delivery, a tested playable copy, these manuals, and the support contact details. Make sure a new developer receives the **complete repository including large assets**, rather than just the visible scripts. Some large files need Git LFS, a download tool explained in the developer guide.

Ask future developers to work on a separate copy/version, explain whether existing saves will still work, provide an updated playable folder and test the Ancient activity on the intended computer before replacing the accepted version. Ask them to update both guides when behaviour changes.

Before accepting the handover, try this short check with the team:

- [ ] Open the supplied game without Unity and start an empty slot.
- [ ] Walk, look, carry a card and change a setting.
- [ ] Complete the introduction, story, wall puzzle and quiz together.
- [ ] Use Save & Quit, close, reopen and Continue; have the team demonstrate what is retained and explain what resets.
- [ ] Confirm the placeholder question, unfinished exhibitions and controller limitations are recorded.
- [ ] Confirm you have repository access, the full game folder, backups and a named person/channel for reporting faults.

The [Developer Handover](DEVELOPER_HANDOVER.md) is the document to pass to whoever develops or repairs the project next.
