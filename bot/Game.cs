﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class Game
    {
        public static long IGameID = 0L;
        public static List<IHero> hero_list = new List<IHero>();
        public static short MaxPageValue = 3;
        public static short MinPageValue = 1;

        public static string smile_hp = "\u2764";
        public static string smile_mp = "🔯";
        public static string smile_dps = "🔥";
        public static string smile_armor = "\u25FB";

        public long GameID;
        private Users.User player_one;
        private Users.User player_two;
        Telegram.Bot.TelegramBotClient bot;

        private IHero hero_one;
        private IHero hero_two;

        public bool isWorking = false;

        public long GetIDofPlayerOne()
        {
            return player_one.ID;
        }

        public long GetIDofPlayerTwo()
        {
            return player_two.ID;
        }

        public static void Initialize()
        {
            // main += 20
            hero_list.Add(new IHero("Juggernaut", 200, 280, 140, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Faceless Void", 230, 250, 150, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Alchemist", 270, 110, 250, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Abaddon", 250, 170, 210, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Lifestealer", 270, 180, 150, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Silencer", 170, 220, 270, IHero.MainFeature.Intel));
            hero_list.Add(new IHero("Wraith King", 240, 180, 180, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Sniper", 160, 230, 150, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Earthshaker", 240, 120, 160, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Slardar", 230, 170, 150, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Razor", 210, 240, 210, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Ursa", 230, 200, 160, IHero.MainFeature.Agi));
        }

        public Game(Users.User user_one, Users.User user_two, Telegram.Bot.TelegramBotClient _bot)
        {
            player_one = user_one;
            player_two = user_two;
            bot = _bot;
            IGameID++;
            GameID = IGameID;

            player_one.ActiveGameID = GameID;
            player_two.ActiveGameID = GameID;

            player_one.status = Users.User.Status.GameConfirming;
            player_two.status = Users.User.Status.GameConfirming;

            isWorking = true;
        }

        public void PickHeroes()
        {
            bot.SendTextMessageAsync(player_one.ID, "Pick hero!");
            player_one.status = Users.User.Status.Picking;
            bot.SendTextMessageAsync(player_two.ID, "Pick hero!");
            player_two.status = Users.User.Status.Picking;
        }

        public IHero Copy(IHero hero)
        {
            return new IHero(hero);
        }

        private void Reset()
        {
            hero_one = null;
            hero_two = null;

            player_one.ActiveGameID = 0L;
            player_two.ActiveGameID = 0L;
            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;
            player_one.HeroName = "";
            player_two.HeroName = "";

            isWorking = false;
        }

        public async void LeaveConfirming(long PlayerID)
        {
            if (PlayerID == player_one.ID)
            {
                await bot.SendTextMessageAsync(PlayerID, player_one.lang.SearchingModeStopped);
                await bot.SendTextMessageAsync(player_two.ID, player_two.lang.PlayerLeftThisLobby);
            }
            else
            {
                await bot.SendTextMessageAsync(PlayerID, player_two.lang.SearchingModeStopped);
                await bot.SendTextMessageAsync(player_one.ID, player_one.lang.PlayerLeftThisLobby);
            }
            Reset();
        }

        public void PickHero(IHero hero, long PlayerID)
        {
            ///// Player one
            if (PlayerID == player_one.ID)
            {
                hero_one = Copy(hero);
                PickHero(player_one, hero_one.Name, player_two);
            }
            ////Player two
            else if (PlayerID == player_two.ID)
            {
                hero_two = Copy(hero);
                PickHero(player_two, hero_two.Name, player_one);
            }
            else
            {
                Reset();
                bot.SendTextMessageAsync(player_one.ID, player_one.lang.PickHeroError);
                bot.SendTextMessageAsync(player_two.ID, player_two.lang.PickHeroError);
            }
        }

        private async void PickHero(Users.User firstPlayer, string heroName, Users.User secondPlayer)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            await bot.SendTextMessageAsync(firstPlayer.ID, $"{firstPlayer.lang.PickedHero} {heroName} !", replyMarkup: kb);
            firstPlayer.HeroName = heroName;

            if (secondPlayer.status == Users.User.Status.Picked)
            {
                Random random = new Random();
                if (random.Next(0, 2) == 0)
                    SetAttackerAndExcepter(firstPlayer, secondPlayer);
                else
                    SetAttackerAndExcepter(secondPlayer, firstPlayer);
            }
            else
            {
                firstPlayer.status = Users.User.Status.Picked;
                await bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.WaitForPickOfAnotherPlayer);
            }
        }

        private async void SetAttackerAndExcepter(Users.User attacker, Users.User excepter)
        {
            attacker.status = Users.User.Status.Attacking;
            excepter.status = Users.User.Status.Excepting;

            await bot.SendTextMessageAsync(attacker.ID, attacker.lang.YourEnemyMessage + ": " + excepter.Name);
            await bot.SendTextMessageAsync(excepter.ID, excepter.lang.YourEnemyMessage + ": " + attacker.Name);

            await SendHeroesStates();

            await bot.SendTextMessageAsync(attacker.ID, attacker.lang.FirstAttackNotify);
            await bot.SendTextMessageAsync(excepter.ID, excepter.lang.EnemyFirstAttackNotify);

            IHero temp = attacker.HeroName == hero_one.Name ? hero_one : hero_two;

            await bot.SendTextMessageAsync(attacker.ID, string.Join("\n", temp.GetMessageAbiliesList(attacker)));
            await bot.SendTextMessageAsync(excepter.ID, excepter.lang.WaitingForAnotherPlayerAction);
        }

        private async void confirmGame(Users.User firstPlayer, bool accepted, Users.User secondPlayer)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            if (accepted)
            {
                await bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameAccepted);
                if (secondPlayer.status == Users.User.Status.WaitingForRespond)
                {
                    firstPlayer.status = Users.User.Status.Picking;
                    secondPlayer.status = Users.User.Status.Picking;

                    await bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameStarted, replyMarkup: kb);
                    await bot.SendTextMessageAsync(secondPlayer.ID, secondPlayer.lang.GameStarted, replyMarkup: kb);

                    string allHero = string.Join("\n", hero_list.Select(x => x.Name));
                    string msg = $"{firstPlayer.lang.StringHeroes}:\n{allHero}\n{firstPlayer.lang.PickHero}:";
                    string msg1 = $"{secondPlayer.lang.StringHeroes}:\n{allHero}\n{secondPlayer.lang.PickHero}:";

                    await bot.SendTextMessageAsync(firstPlayer.ID, msg, replyMarkup: GetKeyboardNextPage(firstPlayer.ID));
                    await bot.SendTextMessageAsync(secondPlayer.ID, msg1, replyMarkup: GetKeyboardNextPage(secondPlayer.ID));
                }
                else
                {
                    firstPlayer.status = Users.User.Status.WaitingForRespond;
                    await bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                }
            }
            else
            {
                Reset();
                await bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameCanceled + "\n" + firstPlayer.lang.GameNotAccepted, replyMarkup: kb);
                await bot.SendTextMessageAsync(secondPlayer.ID, secondPlayer.lang.GameCanceled + "\n" + secondPlayer.lang.AnotherPlayerDidntAcceptGame, replyMarkup: kb);
            }
        }

        public void ConfirmGame(bool accepted, long PlayerID)
        {
            lock (this)
            {
                if (PlayerID == player_one.ID)
                {
                    confirmGame(player_one, accepted, player_two);
                }
                else if (PlayerID == player_two.ID)
                {
                    confirmGame(player_two, accepted, player_one);
                }
            }
        }

        public async void LeaveGame(long PlayerID)
        {
            if (PlayerID == player_one.ID)
            {
                player_one.AddLose();
                await bot.SendTextMessageAsync(player_one.ID, player_one.lang.Retreat);
                await bot.SendTextMessageAsync(player_two.ID, player_two.lang.RetreatEnemy);
            }
            else
            {
                player_one.AddWin();
                await bot.SendTextMessageAsync(player_two.ID, player_two.lang.Retreat);
                await bot.SendTextMessageAsync(player_one.ID, player_one.lang.RetreatEnemy);
            }
            hero_one = null;
            hero_two = null;
            player_one.ActiveGameID = 0L;
            player_two.ActiveGameID = 0L;
            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;
            isWorking = false;
        }

        public async Task<bool> UseAbility(int number, long PlayerID)
        {
            Users.User user_attacker = null;
            Users.User user_excepter = null;
            IHero attacker = null;
            IHero excepter = null;

            if (player_one.ID == PlayerID)
            {
                user_attacker = player_one;
                user_excepter = player_two;

                attacker = hero_one;
                excepter = hero_two;
            }
            else
            {
                user_attacker = player_two;
                user_excepter = player_one;

                attacker = hero_two;
                excepter = hero_one;
            }

            bool finished = false;

            switch (number)
            {
                case 1:
                    if (await attacker.Attack(excepter, user_attacker, user_excepter))
                            finished = true;
                        break;
                case 2:
                    if (await attacker.Heal(user_attacker, user_excepter))
                            finished = true;
                        break;
                case 3:
                    if (await attacker.UseAbilityOne(user_attacker, user_excepter, excepter))
                        finished = true;
                    break;
                case 4:
                    if (await attacker.UseAbilityTwo(user_attacker, user_excepter, excepter))
                        finished = true;
                    break;
            }

            if (finished)
            {
                hero_one.Update();
                hero_two.Update();

                if (Math.Floor(hero_one.HP) <= 0.0f || Math.Floor(hero_two.HP) <= 0.0f)
                {
                    if (Math.Floor(hero_one.HP) <= 0.0f)
                        GameOver(hero_two, hero_one, player_two, player_one);
                    else
                        GameOver(hero_one, hero_two, player_one, player_two);
                    return true;
                }

                await bot.SendTextMessageAsync(user_attacker.ID, $"{GetMessageForMe(user_attacker.lang, attacker)}\n\n{GetMessageForEnemy(user_attacker.lang, excepter)}");
                await bot.SendTextMessageAsync(user_excepter.ID, $"{GetMessageForMe(user_excepter.lang, excepter)}\n\n{GetMessageForEnemy(user_excepter.lang, attacker)}");

                if (player_one.status == Users.User.Status.Attacking)
                {
                    //Console.WriteLine("one");
                    if (hero_two.StunCounter == 0)
                    {
                        user_attacker.status = Users.User.Status.Excepting;
                        user_excepter.status = Users.User.Status.Attacking;

                        await bot.SendTextMessageAsync(user_excepter.ID, string.Join("\n", excepter.GetMessageAbiliesList(
                            user_excepter)));
                        await bot.SendTextMessageAsync(user_attacker.ID, user_attacker.lang.WaitingForAnotherPlayerAction);
                    }
                    else
                        await bot.SendTextMessageAsync(user_attacker.ID, string.Join("\n", attacker.GetMessageAbiliesList(
                            user_attacker)));
                }
                else
                {
                    //Console.WriteLine("Two");
                    if (hero_one.StunCounter == 0)
                    {
                        user_attacker.status = Users.User.Status.Excepting;
                        user_excepter.status = Users.User.Status.Attacking;

                        await bot.SendTextMessageAsync(user_excepter.ID, string.Join("\n", excepter.GetMessageAbiliesList(
                            user_excepter)));
                        await bot.SendTextMessageAsync(user_attacker.ID, user_attacker.lang.WaitingForAnotherPlayerAction);
                    }
                    else
                        await bot.SendTextMessageAsync(user_attacker.ID, string.Join("\n", attacker.GetMessageAbiliesList(
                            user_attacker)));
                }
                //Console.WriteLine("End.");
                attacker.UpdateStunDuration();
                excepter.UpdateStunDuration();
                return true;
            }
            else
                return false;
        }

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage(long PlayerID)
        {
            Users.User user = PlayerID == player_one.ID ? player_one : player_two;

            if (user.HeroListPage >= MaxPageValue)
                user.HeroListPage = MaxPageValue;
            else
                user.HeroListPage++;

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.OneTimeKeyboard = true;
            keyboard.ResizeKeyboard = true;

            switch (user.HeroListPage)
            {
                case 1:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Juggernaut"),
                            new Telegram.Bot.Types.KeyboardButton("Faceless Void")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Alchemist"),
                            new Telegram.Bot.Types.KeyboardButton("Abaddon")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 2:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Lifestealer"),
                            new Telegram.Bot.Types.KeyboardButton("Silencer")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Wraith King"),
                            new Telegram.Bot.Types.KeyboardButton("Sniper")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<"),
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 3:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Earthshaker"),
                            new Telegram.Bot.Types.KeyboardButton("Slardar")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Razor"),
                            new Telegram.Bot.Types.KeyboardButton("Ursa")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<")
                        }
                    };
                    break;
            }
            return keyboard;
        }

        private async void GameOver(IHero winner, IHero loser, Users.User uwinner, Users.User uloser)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            string[] msg1 =
            {
                $"{uwinner.Name} ({winner.Name}) {uwinner.lang.HasWonThisBattle}!",
                $"{uloser.Name} ({loser.Name}) {uwinner.lang.HasLostThisBattle}!",
                $"{uwinner.lang.Result}:",
            };
            string[] msg2 =
            {
                $"{uwinner.Name} ({winner.Name}) {uloser.lang.HasWonThisBattle}!",
                $"{uloser.Name} ({loser.Name}) {uloser.lang.HasLostThisBattle}!",
                $"{uloser.lang.Result}:",
            };
            uwinner.AddWin();
            uloser.AddLose();

            await bot.SendTextMessageAsync(uwinner.ID, uwinner.lang.GameFinished, replyMarkup: kb);
            await bot.SendTextMessageAsync(uloser.ID, uloser.lang.GameFinished, replyMarkup: kb);

            await bot.SendTextMessageAsync(uwinner.ID, string.Join("\n", msg1));
            await bot.SendTextMessageAsync(uloser.ID, string.Join("\n", msg2));

            await bot.SendTextMessageAsync(uwinner.ID, GetMessageForMe(uwinner.lang, winner));
            await bot.SendTextMessageAsync(uloser.ID, GetMessageForMe(uloser.lang, loser));
            await bot.SendTextMessageAsync(uwinner.ID, GetMessageForEnemy(uwinner.lang, loser));
            await bot.SendTextMessageAsync(uloser.ID, GetMessageForEnemy(uloser.lang, winner));

            winner = null;
            loser = null;

            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;

            isWorking = false;
        }

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardPrevPage(long PlayerID)
        {
            Users.User user = PlayerID == player_one.ID ? player_one : player_two;

            if (user.HeroListPage <= MinPageValue)
                user.HeroListPage = MinPageValue;
            else
                user.HeroListPage--;

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.OneTimeKeyboard = true;
            keyboard.ResizeKeyboard = true;

            switch (user.HeroListPage)
            {
                case 1:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Juggernaut"),
                            new Telegram.Bot.Types.KeyboardButton("Faceless Void")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Alchemist"),
                            new Telegram.Bot.Types.KeyboardButton("Abaddon")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 2:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Lifestealer"),
                            new Telegram.Bot.Types.KeyboardButton("Silencer")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Wraith King"),
                            new Telegram.Bot.Types.KeyboardButton("Sniper")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<"),
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 3:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Earthshaker"),
                            new Telegram.Bot.Types.KeyboardButton("Slardar")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Razor"),
                            new Telegram.Bot.Types.KeyboardButton("Ursa")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<")
                        }
                    };
                    break;
            }
            return keyboard;
        }

        private static string GetMessageForMe(Users.User.Text playerLang, IHero playerHero)
        {
            string[] lines =
                {
                    playerLang.YouMessage,
                    $"{playerLang.HeroNameMessage}: {playerHero.Name}",
                    $"{playerLang.HpText}: {Convert.ToInt32(playerHero.HP)}/{Convert.ToInt32(playerHero.MaxHP)} {smile_hp}",
                    $"{playerLang.MpText}: {Convert.ToInt32(playerHero.MP)}/{Convert.ToInt32(playerHero.MaxMP)} {smile_mp}",
                    $"{playerLang.DpsText}: {Convert.ToInt32(playerHero.DPS)} {smile_dps}",
                    $"{playerLang.ArmorText}: {Convert.ToInt32(playerHero.Armor)} {smile_armor}",
                };

            return string.Join("\n", lines);
        }

        private static string GetMessageForEnemy(Users.User.Text playerLang, IHero enemyHero)
        {
            string[] lines =
            {
                playerLang.YourEnemyMessage,
                $"{playerLang.HeroNameMessage}: {enemyHero.Name}",
                $"{playerLang.HpText}: {Convert.ToInt32(enemyHero.HP)}/{Convert.ToInt32(enemyHero.MaxHP)} {smile_hp}",
                $"{playerLang.MpText}: {Convert.ToInt32(enemyHero.MP)}/{Convert.ToInt32(enemyHero.MaxMP)} {smile_mp}",
                $"{playerLang.DpsText}: {Convert.ToInt32(enemyHero.DPS)} {smile_dps}",
                $"{playerLang.ArmorText}: {Convert.ToInt32(enemyHero.Armor)} {smile_armor}",
            };

            return string.Join("\n", lines);
        }

        private async Task SendHeroesStates()
        {
            await bot.SendTextMessageAsync(player_one.ID, GetMessageForMe(player_one.lang, hero_one));
            await bot.SendTextMessageAsync(player_two.ID, GetMessageForMe(player_two.lang, hero_two));


            await bot.SendTextMessageAsync(player_one.ID, GetMessageForEnemy(player_one.lang, hero_two));
            await bot.SendTextMessageAsync(player_two.ID, GetMessageForEnemy(player_two.lang, hero_one));
        }
    }
}
