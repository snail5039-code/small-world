using System.Collections.Generic;
using SmallWorld.Dialogue.Stage7;

namespace SmallWorld.UI.Stage7
{
    public static class Stage7DemoDialogue
    {
        public const string RelationshipKey = "mira_relationship";

        public static DialogueDefinition Create()
        {
            return new DialogueDefinition("reality_room_demo", "welcome", new[]
            {
                new DialogueNode("welcome", "미라", "왔구나. 이 방이 조금 낯설지?", "question", autoAdvanceSeconds: 2.5f),
                new DialogueNode("question", "미라", "그래도 나와 함께 둘러볼래?", choices: new[]
                {
                    new DialogueChoice("trust", "응, 네가 있어서 안심돼.", "warm",
                        effects: new[] { new DialogueEffect(RelationshipKey, 2) }),
                    new DialogueChoice("doubt", "아직은 잘 모르겠어.", "cold",
                        effects: new[] { new DialogueEffect(RelationshipKey, -1) })
                }),
                new DialogueNode("warm", "미라", "고마워. 천천히 전부 보여 줄게.", "farewell"),
                new DialogueNode("cold", "미라", "괜찮아. 서두르지 않아도 돼.", "farewell"),
                new DialogueNode("farewell", "미라", "준비가 되면 방 안을 살펴봐.")
            });
        }
    }
}
