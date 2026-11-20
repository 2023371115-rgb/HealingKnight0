using System.Collections.Generic;
using HealingKnight.Core.Progression;
using NUnit.Framework;
using UnityEngine;

namespace HealingKnight.Tests
{
    public class ProgressServiceTests
    {
        private GameCatalog catalog;
        private SystemData digestivo, respiratorio;
        private LevelData boca, estomago, traquea, secreto;
        private readonly List<Object> created = new List<Object>();

        private T Make<T>() where T : ScriptableObject
        {
            T obj = ScriptableObject.CreateInstance<T>();
            created.Add(obj);
            return obj;
        }

        private LevelData Level(string id, string scene, bool formula = false)
        {
            LevelData l = Make<LevelData>();
            l.id = id;
            l.sceneName = scene;
            l.grantsFormula = formula;
            return l;
        }

        [SetUp]
        public void SetUp()
        {
            boca = Level("boca", "Digestivo_Boca");
            estomago = Level("estomago", "Digestivo_Estomago", true);
            traquea = Level("traquea", "");
            secreto = Level("secreto", "Digestivo_Secreto");
            secreto.hidden = true;
            secreto.secretId = "pasaje";

            digestivo = Make<SystemData>();
            digestivo.id = "digestivo";
            digestivo.levels = new List<LevelData> { boca, estomago, secreto };

            respiratorio = Make<SystemData>();
            respiratorio.id = "respiratorio";
            respiratorio.levels = new List<LevelData> { traquea };

            catalog = Make<GameCatalog>();
            catalog.systems = new List<SystemData> { digestivo, respiratorio };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void PartidaNueva_SoloElPrimerNivelEstaDisponible()
        {
            var progress = new ProgressService(catalog, new SaveData());
            Assert.IsTrue(progress.IsSystemUnlocked(digestivo));
            Assert.IsFalse(progress.IsSystemUnlocked(respiratorio));
            Assert.IsTrue(progress.IsLevelPlayable(boca));
            Assert.IsFalse(progress.IsLevelPlayable(estomago));
        }

        [Test]
        public void CompletarNivel_DesbloqueaElSiguiente()
        {
            var progress = new ProgressService(catalog, new SaveData());
            progress.CompleteLevel(boca);
            Assert.IsTrue(progress.IsLevelPlayable(estomago));
            Assert.IsTrue(progress.IsLevelPlayable(boca), "Lo completado se puede rejugar");
        }

        [Test]
        public void CompletarSistema_DesbloqueaElSiguienteSistemaYDaFormula()
        {
            var save = new SaveData();
            var progress = new ProgressService(catalog, save);
            progress.CompleteLevel(boca);
            progress.CompleteLevel(estomago);

            Assert.IsTrue(progress.IsSystemCompleted(digestivo));
            Assert.IsTrue(progress.IsSystemUnlocked(respiratorio));
            CollectionAssert.Contains(save.formulas, "digestivo");
        }

        [Test]
        public void NivelSinEscena_NoEsJugableAunqueEsteDesbloqueado()
        {
            var progress = new ProgressService(catalog, new SaveData());
            progress.CompleteLevel(boca);
            progress.CompleteLevel(estomago);
            Assert.IsTrue(progress.IsLevelUnlocked(traquea));
            Assert.IsFalse(progress.IsLevelPlayable(traquea));
        }

        [Test]
        public void CompletarDosVeces_NoDuplica()
        {
            var save = new SaveData();
            var progress = new ProgressService(catalog, save);
            Assert.IsTrue(progress.CompleteLevel(boca));
            Assert.IsFalse(progress.CompleteLevel(boca));
            Assert.AreEqual(1, save.completedLevels.Count);
        }

        [Test]
        public void NivelOculto_NoSeVeNiCuentaHastaEncontrarElSecreto()
        {
            var save = new SaveData();
            var progress = new ProgressService(catalog, save);

            Assert.IsFalse(progress.IsLevelVisible(secreto));
            Assert.IsFalse(progress.IsLevelPlayable(secreto));

            // Completar los niveles normales basta para terminar el sistema (el secreto es opcional).
            progress.CompleteLevel(boca);
            progress.CompleteLevel(estomago);
            Assert.IsTrue(progress.IsSystemCompleted(digestivo));

            save.AddSecret("pasaje");
            Assert.IsTrue(progress.IsLevelVisible(secreto));
            Assert.IsTrue(progress.IsLevelPlayable(secreto));
        }

        [Test]
        public void Guardado_SobreviveSerializacionJson()
        {
            var save = new SaveData();
            save.MarkLevelCompleted("boca");
            save.AddFormula("digestivo");
            save.prologueSeen = true;
            save.AddSecret("pasaje");

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            Assert.IsTrue(loaded.IsLevelCompleted("boca"));
            Assert.IsTrue(loaded.prologueSeen);
            CollectionAssert.Contains(loaded.formulas, "digestivo");
            Assert.IsTrue(loaded.HasSecret("pasaje"));
        }
    }
}
