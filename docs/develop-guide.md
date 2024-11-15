# StepUpTableTennis 開発ガイド

## 1. プロジェクト概要

卓球のMRトレーニングアプリケーション。スキル評価、脳視覚刺激トレーニング、AI対戦の3モードを予定。
現在はスキル評価モードの開発に注力。

### メインモード：スキル評価モード

- 総合トレーニング
- スピードトレーニング
- スピントレーニング
- コーストレーニング
- カスタムトレーニング

## 2. 技術スタック

### 使用ライブラリ

- **UniTask**: 非同期処理
- **VContainer**: DI（依存性注入）
- **R3**: リアクティブプログラミング
- **MessagePipe**: メッセージング
- **NaughtyAttributes**: インスペクタ拡張
- **Unity Input System**: 入力管理
- **Meta XR SDK**: MR機能
- **NuGetForUnity**: NuGetパッケージ管理

### シーン構成

- Main.unity（単一シーン構成）

## 3. プロジェクト構造

```
Assets/
├── _Project/
│   ├── Prefabs/        # プレハブ
│   ├── Scenes/         # シーン
│   ├── Scripts/        # スクリプト
│   ├── ScriptableObjects/ # 設定データ
│   └── Tests/          # テストコード
```

### Scripts/の詳細構造

```
Scripts/
├── Core/              # コアロジック
│   ├── Domain/        # ドメインモデル
│   ├── Application/   # アプリケーションサービス
│   └── Infrastructure/# インフラ層
├── Presentation/      # UI関連
├── Features/          # 機能モジュール
├── Common/            # 共通機能
└── DI/                # DI設定
```

## 4. コーディング規約

### 命名規則

- **クラス名**: PascalCase (`PlayerController`)
- **メソッド名**: PascalCase (`CalculateScore()`)
- **変数名**: camelCase (`playerScore`)
- **定数名**: SNAKE_CASE (`MAX_PLAYERS`)
- **インターフェース名**: 'I'プレフィックス (`IScoreCalculator`)
- **プライベートフィールド**: アンダースコアプレフィックス (`_score`)

### ネームスペース

```csharp
namespace StepUpTableTennis.{Category}.{Subcategory}
```

## 5. 開発ガイドライン

### 必ず守ること

1. **DIの活用**

   - `FindObjectOfType`の使用禁止
   - コンストラクタインジェクションを優先
   - MonoBehaviourの場合は `[Inject]`属性付きのメソッドインジェクション
2. **非同期処理**

   - `async/await`（UniTask）を優先使用
   - コルーチンは必要な場合のみ
3. **イベント処理**

   - MessagePipeを使用
   - UnityEventは使用しない
   - R3を活用したリアクティブな実装
4. **設定データ**

   - ScriptableObjectを活用
   - 直接値をInspectorに設定しない
5. **テスト**

   - 新機能追加時は必ずテストを作成
   - PlayModeテストとEditModeテストを適切に使い分け

### アンチパターン（避けるべきこと）

- シングルトンパターンの使用
- `FindObjectOfType`/`Find`の使用
- `MonoBehaviour`の不必要な継承
- パブリックフィールドの露出
- 直接的なGameObject/Component参照

### パフォーマンス考慮点

- オブジェクトプーリングの活用
- GC抑制（構造体の活用）
- Update内での処理を最小限に

## 6. DI設定例

```csharp
public class TrainingLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // インターフェースと実装の紐付け
        builder.Register<ITableTennisPhysics, TableTennisPhysicsImpl>(Lifetime.Singleton);
      
        // コンポーネント登録
        builder.RegisterComponent(new TrainingSystem());
      
        // EntryPoint登録
        builder.RegisterEntryPoint<GameInitializer>();
    }
}
```

## 7. イベント処理例

```csharp
// イベント定義
public readonly struct TrainingEvent
{
    public string EventType { get; }
    public object Data { get; }
}

// Publisher
IPublisher<TrainingEvent> _publisher;
_publisher.Publish(new TrainingEvent("BallLaunched", data));

// Subscriber
ISubscriber<TrainingEvent> _subscriber;
_subscriber.Subscribe(ev => HandleTrainingEvent(ev));
```

## 8. テスト記述例

```csharp
[Test]
public async UniTask TestBallLaunch()
{
    // Arrange
    var physics = new TableTennisPhysicsImpl();
    var ball = new BallController(physics);
  
    // Act
    await ball.LaunchAsync(startPos, targetPos);
  
    // Assert
    Assert.That(ball.Position, Is.Not.EqualTo(startPos));
}
```

## 9. 進行中の優先タスク

1. 基本的な物理演算の実装
2. ボール発射機能の実装
3. データ記録システムの構築
4. トレーニングモードの基本UI

## 10. 追加リソース

- [VContainer documentation](https://vcontainer.hadashikick.jp/)
- [UniTask documentation](https://github.com/Cysharp/UniTask)
- [R3 documentation](https://github.com/Cysharp/R3)
- [MessagePipe documentation](https://github.com/Cysharp/MessagePipe)
- [Input System documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
