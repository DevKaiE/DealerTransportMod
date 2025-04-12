namespace PackagerExtension.Utils
{
    public static class Messages
    {
        public static List<string> DealerItemCollectionMessages = new List<string>
        {
            "Yo boss! Just hit the stash. This shit's fire, no cap.",
            "Grabbed that product from the spot. Five-O ain't see nothin'.",
            "Storage run complete. Got that good-good for the fiends.",
            "Snatched some merch from the back. We movin' weight tonight!",
            "Just re-upped from storage. Clientele been blowin' up my phone.",
            "Raided the stash spot. We 'bout to run the block with this batch.",
            "Yo, I just touched the package. This that top-shelf shit right here.",
            "Storage run was clean. Ain't nobody peepin' our moves.",
            "Got that work from the back. Time to flood the streets, ya dig?",
            "Just secured the bag from storage. We eatin' good tonight!",
            "Product secured, ready to slang. These streets gonna pay us, feel me?",
            "Took that heat from storage. Trap about to be jumpin'!",
            "Got that pack from the back. Fiends gonna be lined up 'round the block.",
            "Straight murked that storage inventory. We 'bout to make it rain!",
            "Finessed some product from storage. Time to tax these customers.",
            "Storage run complete. Got that gas that'll have 'em coming back.",
            "Just hit a lick on our own stash. That's smart business, ya heard?",
            "Got them thangs from storage. Custies better have my paper ready.",
            "Inventory grabbed. Bout to flip this shit and double up.",
            "Storage run was a success. We pushin' P tonight for real!",
        };

        public static List<string> DealerNoItemsFoundMessages = new List<string>
        {
            "Yo, storage is bone dry. Can't make money with empty hands, boss.",
            "Ain't shit in the stash! How we supposed to eat?",
            "Storage lookin' weak as hell. No product, no profit, feel me?",
            "Stash spot empty. These streets ain't gonna wait for us to re-up.",
            "Bruh, storage is a ghost town. Custies gonna start hittin' up the competition.",
            "Storage run was a bust. Can't hustle with air, ya dig?",
            "Nothin' in the back but dust and disappointment. We lookin' soft out here.",
            "Storage empty as my pockets before this job. We need to fix that ASAP.",
            "Can't find what I need in this bitch. How we supposed to trap with no pack?",
            "Yo, storage situation is FUBAR. Need that re-up yesterday.",
            "Stash spsot drier than the Sahara. We 'bout to lose our corner if we don't re-up.",
            "Storage run was dead. No product means no paper, and that's bad business.",
            "Came up empty-handed. The block gonna think we fell off if we don't re-up soon.",
            "Storage got nothin' I can push. Fiends blowin' up my phone for nothin'.",
            "Stash is straight garbage. Can't serve the customers with empty bags.",
            "Storage ain't hittin'. Need that work or we'll be the ones lookin' for work.",
            "This empty storage shit is bad for business. Streets talk, and they sayin' we slippin'.",
            "Went to grab product and came back with fuck all. We need to re-up, boss.",
            "Storage situation is trash. Can't be a player if we ain't got no game to sell.",
            "Stash run was a fail. Competition gonna eat our lunch if we don't stock up."
        };

        public static string GetRandomItemCollectionMessage(bool success)
        {
            if (success)
            {
                int index = UnityEngine.Random.Range(0, DealerItemCollectionMessages.Count);
                return DealerItemCollectionMessages[index];
            }
            else
            {
                int index = UnityEngine.Random.Range(0, DealerNoItemsFoundMessages.Count);
                return DealerNoItemsFoundMessages[index];
            }
        }
    }
}
