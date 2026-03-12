//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

/*

Undo systm TODO list:
---------------------
- DONE: Global reset prev values (ResetPrevVal())
- DONE: action: render pass: add new
- DONE: action: sdf object: change object type
- DONE: action: sdf object: change mix operator
- DONE: action: render pass: change renderer type
- DONE: (verify): clear undo stack after new/load project
- DONE: (verify): action: all other combo box'es
- DONE: action: add operator (to OperatorsCollection)
- DONE: action: remove operator (to OperatorsCollection)
- DONE: action: move up operator (OperatorsCollection)      indexForMoveUp
- DONE: action: move down operator (OperatorsCollection)    indexForMoveDown
- DONE: action: sdf object: move up
- DONE: action: sdf object: move down
- DONE: action: material: move up
- DONE: action: material: move down
- DONE: action: render pass: move up
- DONE: action: render pass: move down

- TODO: action: render pass: cut
- TODO: action: render pass: paste
- TODO: (low priority) history window (photoshop style)

- Pomysł na uniwersalne move up/down (okazuje się że move up/down z grubsza tak działa):
    - przenieść tą funkcjonalność do klasy TreeNode, robić to bezpośrednio na: TreeNode.Children
    - trzeba tylko jakoś przekazywać funkcję do wywołania "after move"
    - w ten sposób każdy TreeNode będzie mógł być przenoszony up/down
    - TreeNode powinien mieć cechę pozwalającą na przemieszczanie go w hierarchii "IsMovable"
    - na tej podstawie UI mogło by dodawać opcje do przenoszenia węzłów.

- TODO: na kiedyś: rozważyć wpięcie PositionOperators i DistanceOperators w hierarchię TreeNode

- KNOWN ISSUES:
    - Usuwanie materiału:
        gdy usunie się materiał, wszystkie referencje na ten materiał w obiektach są korygowane.
        ta korekcja nie jest zapisywana w systemie undo/redo, więc po cofnięciu akcji
        referencje nie zostaną przywrócone.
        Pomysły na rozwiązania:
        - zapisywać obiekty które mają referencje na usuwany materiał. Przy undo przywrócić w nich referencje
        - undo/redo poprzez serializację całego modelu (raczej słaby pomysł)
        - nie pozwalać usunąć materiału który jest używany przez jakikolwiek obiekt
        - zapisywać zmiany referencji na materiały w stosie undo jako osobne akcje
        - a może ustawienie materiału w obiekcie traktować jako "preferowany" materiał. 
            A jeśli takiego materiału nie ma, to nie korygować wpisu, tylko używać default 
            (i jakoś zaznaczać to w UI, że brakuje materiału)
*/

namespace SdfGlueCore.UndoSystem
{
    public class UndoManager
    {
        private Stack<UndoAction>        undoStack_     = new Stack<UndoAction>();
        private Stack<UndoAction>        redoStack_     = new Stack<UndoAction>();

        private static UndoManager      instance_ = new UndoManager();

        public static UndoManager Instance
        {
            get
            {
                //if (instance_ == null)
                //    instance_ = new UndoManager();

                return instance_;
            }
        }

        public void ClearAll()
        {
            undoStack_.Clear();
            redoStack_.Clear();
        }

        public void DoUndo()
        {
            if (undoStack_.Count == 0)
                return;

            UndoAction act = undoStack_.Pop();
            redoStack_.Push(act);
            act.ApplyUndo();
        }

        public void DoRedo()
        {
            if (redoStack_.Count == 0)
                return;

            UndoAction act = redoStack_.Pop();
            undoStack_.Push(act);
            act.ApplyRedo();
        }

        public void SaveAction(UndoAction action)
        {
            undoStack_.Push(action);
            redoStack_.Clear();
        }

    }
}
