<script setup>
import { reactive, ref, computed, watch, onMounted } from 'vue'
import { api } from './api.js'

const TxType = { Income: 'Income', Expense: 'Expense' }
const fmtDate = s => (s ? String(s).slice(0, 10) : '')

const state = reactive({
  wallets: [],
  reportYm: new Date().toISOString().slice(0, 7),
  reportCurrency: '',
  ui: { expanded: {} },
  loading: false,
  error: ''
})

const currencyOptions = computed(() =>
  Array.from(new Set(state.wallets.map(w => (w.currency || '').trim()).filter(Boolean))).sort()
)

function currentBalance (wallet) {
  const inc = wallet.transactions.filter(t => t.type === TxType.Income).reduce((s, t) => s + Number(t.amount), 0)
  const exp = wallet.transactions.filter(t => t.type === TxType.Expense).reduce((s, t) => s + Number(t.amount), 0)
  return Number(wallet.initialBalance) + inc - exp
}

async function loadWallets () {
  state.loading = true; state.error = ''
  try {
    state.wallets = await api.getWallets()
  } catch (e) {
    state.error = 'Не удалось загрузить кошельки: ' + (e?.message ?? e)
  } finally { state.loading = false }
}
onMounted(loadWallets)

const newWallet = reactive({ name: '', currency: 'RUB', initialBalance: 0 })
async function addWallet () {
  if (!newWallet.name.trim()) return
  try {
    const created = await api.createWallet({
      name: newWallet.name.trim(),
      currency: newWallet.currency.trim() || 'RUB',
      initialBalance: Number(newWallet.initialBalance) || 0,
      transactions: []
    })
    state.wallets.push(created)
    Object.assign(newWallet, { name: '', currency: 'RUB', initialBalance: 0 })
  } catch (e) {
    state.error = 'Не удалось добавить кошелёк: ' + (e?.message ?? e)
  }
}

const txForm = reactive({
  walletId: '',
  date: new Date().toISOString().slice(0, 10),
  type: TxType.Expense,
  amount: '',
  description: ''
})
const txError = ref('')

async function submitTx () {
  txError.value = ''
  if (!txForm.walletId) { txError.value = 'Выберите кошелёк'; return }
  const wallet = state.wallets.find(w => w.id === txForm.walletId)
  if (!wallet) { txError.value = 'Wallet not found'; return }

  try {
    const created = await api.addTx(wallet.id, {
      date: txForm.date || new Date().toISOString().slice(0, 10),
      amount: Number(txForm.amount || 0),
      type: txForm.type,
      description: (txForm.description || '').trim()
    })
    wallet.transactions.push(created)
    Object.assign(txForm, {
      walletId: wallet.id,
      date: new Date().toISOString().slice(0, 10),
      type: TxType.Expense,
      amount: '',
      description: ''
    })
  } catch (e) {
    txError.value = 'Не удалось добавить транзакцию: ' + (e?.message ?? e)
  }
}

const reportServer = ref(null)
const reportLoading = ref(false)

async function loadReport () {
  if (!state.reportYm) { reportServer.value = null; return }
  const [year, month] = state.reportYm.split('-').map(Number)
  reportLoading.value = true
  try {
    const raw = await api.getReport(year, month, state.reportCurrency.trim() || undefined)
    reportServer.value = normalizeReport(raw)
  } catch (e) {
    reportServer.value = null
    state.error = 'Не удалось загрузить отчёт: ' + (e?.message ?? e)
  } finally { reportLoading.value = false }
}
watch(() => [state.reportYm, state.reportCurrency], loadReport)
onMounted(loadReport)

function normalizeReport (r) {
  if (!r) return null
  const groups = (r.groups ?? r.Groups ?? []).map(g => {
    const items = (g.items ?? g.Items ?? []).map(it => ({
      id: it.id ?? it.Id,
      date: it.date ?? it.Date,
      amount: Number(it.amount ?? it.Amount ?? 0),
      walletName: it.walletName ?? it.WalletName ?? '',
      currency: it.currency ?? it.Currency ?? '', 
      description: it.description ?? it.Description ?? ''
    }))
    return {
      currency: g.currency ?? g.Currency ?? items[0]?.currency ?? '',
      type: g.type ?? g.Type,
      total: Number(g.total ?? g.Total ?? 0),
      items
    }
  })
  const topByWallet = (r.topByWallet ?? r.TopByWallet ?? []).map(b => ({
    wallet: {
      id: b.wallet?.id ?? b.Wallet?.Id,
      name: b.wallet?.name ?? b.Wallet?.Name ?? '',
      currency: b.wallet?.currency ?? b.Wallet?.Currency ?? '',
      currentBalance: Number(b.wallet?.currentBalance ?? b.Wallet?.CurrentBalance ?? 0)
    },
    top: (b.top ?? b.Top ?? []).map(t => ({
      id: t.id ?? t.Id,
      date: t.date ?? t.Date,
      amount: Number(t.amount ?? t.Amount ?? 0),
      description: t.description ?? t.Description ?? ''
    }))
  }))
  return { year: r.year ?? r.Year, month: r.month ?? r.Month, groups, topByWallet }
}

function downloadJson () {
  const data = JSON.stringify(state.wallets, null, 2)
  const blob = new Blob([data], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = Object.assign(document.createElement('a'), { href: url, download: 'finance.json' })
  document.body.appendChild(a); a.click(); a.remove(); URL.revokeObjectURL(url)
}

const importInput = ref(null)
function openImport () { importInput.value?.click() }
async function onImportJson (e) {
  const file = e.target?.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = async () => {
    try {
      const text = String(reader.result ?? '')
      const parsed = JSON.parse(text)
      if (Array.isArray(parsed)) {
        await api.setWallets(parsed)
        await Promise.all([loadWallets(), loadReport()])
      } else {
        alert('Ожидается массив кошельков')
      }
    } catch (err) {
      alert('Ошибка импорта: ' + (err?.message ?? err))
    } finally {
      e.target.value = ''
    }
  }
  reader.readAsText(file, 'utf-8')
}

async function generateSample () {
  try {
    await api.generateSample()
    await Promise.all([loadWallets(), loadReport()])
  } catch (e) {
    state.error = 'Не удалось сгенерировать данные: ' + (e?.message ?? e)
  }
}
</script>

<template>
  <div class="container vstack" style="gap:16px">
    <header class="vstack">
      <h1>Личный учёт</h1>
      <div class="small">
        <span v-if="state.loading">Загрузка…</span>
        <span v-if="state.error" class="alert">{{ state.error }}</span>
      </div>
    </header>
    <div class="panel">
      <div class="actions-grid">
        <button class="btn btn--primary btn--block" @click="generateSample">Сгенерировать данные</button>
        <button class="btn btn--primary btn--block" @click="downloadJson">Экспорт JSON</button>
        <button class="btn btn--primary btn--block" @click="openImport">Импорт JSON</button>
        <input ref="importInput" type="file" accept="application/json" @change="onImportJson" style="display:none">
      </div>
    </div>

    <section class="panel">
      <div class="section-title">Кошельки</div>

      <div class="grid" style="margin-bottom: 12px">
        <div class="col-4 vstack">
          <label>Название</label>
          <input v-model="newWallet.name" placeholder="Напр. Наличные">
        </div>
        <div class="col-4 vstack">
          <label>Валюта</label>
          <input v-model="newWallet.currency" placeholder="RUB / USD">
        </div>
        <div class="col-4 vstack">
          <label>Начальный баланс</label>
          <input v-model.number="newWallet.initialBalance" type="number" step="0.01" min="0">
        </div>
        <div class="col-12">
          <button class="btn btn--primary btn--block" @click="addWallet">Добавить кошелёк</button>
        </div>
      </div>

      <div v-if="!state.wallets.length" class="small">Кошельков пока нет.</div>

      <div v-for="w in state.wallets" :key="w.id" class="panel">
        <div class="wallet-row">
          <strong class="wallet-name">{{ w.name }}</strong>
          <span class="badge">ID: {{ (w.id ?? '').toString().slice(0,8) }}</span>
          <span class="badge">Валюта: {{ w.currency }}</span>
          <span class="badge ok">Баланс: {{ currentBalance(w).toFixed(2) }} {{ w.currency }}</span>
          <span class="small muted">{{ w.transactions?.length ?? 0 }} транзакций</span>
          <button class="btn btn--ghost btn--sm wallet-toggle"
            @click="state.ui.expanded[w.id] = !state.ui.expanded[w.id]">
            {{ state.ui.expanded[w.id] ? 'Скрыть' : 'Показать' }} транзакции
          </button>
        </div>

        <div v-if="state.ui.expanded[w.id]" class="vstack" style="margin-top:10px">
          <table class="table">
            <thead>
              <tr>
                <th>Дата</th>
                <th>Тип</th>
                <th class="right">Сумма</th>
                <th>Описание</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in [...(w.transactions||[])].sort((a,b)=> new Date(a.date)-new Date(b.date))" :key="t.id">
                <td>{{ fmtDate(t.date) }}</td>
                <td><span :class="['badge', t.type===TxType.Income?'ok':'warn']">{{ t.type }}</span></td>
                <td class="right">{{ Number(t.amount).toFixed(2) }} {{ w.currency }}</td>
                <td>{{ t.description || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>
    <section class="panel">
      <div class="section-title">Новая транзакция</div>
      <div class="grid">
        <div class="col-4 vstack">
          <label>Кошелёк</label>
          <select v-model="txForm.walletId">
            <option disabled value="">— выберите —</option>
            <option v-for="w in state.wallets" :key="w.id" :value="w.id">
              {{ w.name }} — {{ currentBalance(w).toFixed(2) }} {{ w.currency }}
            </option>
          </select>
        </div>

        <div class="col-4 vstack">
          <label>Дата</label>
          <input v-model="txForm.date" type="date">
        </div>

        <div class="col-4 vstack">
          <label>Тип</label>
          <select v-model="txForm.type">
            <option value="Expense">Expense</option>
            <option value="Income">Income</option>
          </select>
        </div>

        <div class="col-6 vstack">
          <label>Сумма</label>
          <input v-model="txForm.amount" type="number" step="0.01" min="0">
        </div>

        <div class="col-6 vstack">
          <label>Описание</label>
          <input v-model="txForm.description" placeholder="необязательно">
        </div>

        <div class="col-12">
          <button class="btn btn--primary btn--block" @click="submitTx"
            :disabled="!txForm.walletId || Number(txForm.amount) <= 0">
            Добавить транзакцию
          </button>
          <span v-if="txError" class="alert">{{ txError }}</span>
        </div>
      </div>
    </section>
    <section class="panel">
      <div class="section-title">Отчёт за месяц</div>
      <div class="filters-grid">
        <div class="vstack">
          <label>Месяц</label>
          <input v-model="state.reportYm" type="month">
        </div>
        <div class="vstack">
          <label>Валюта</label>
          <select v-model="state.reportCurrency">
            <option value="">Все валюты</option>
            <option v-for="c in currencyOptions" :key="c" :value="c">{{ c }}</option>
          </select>
        </div>
        <div class="vstack align-end">
          <button class="btn btn--ghost" @click="loadReport" :disabled="reportLoading">
            {{ reportLoading ? 'Обновляем…' : 'Обновить отчёт' }}
          </button>
        </div>
      </div>

      <template v-if="reportServer">
        <div class="vstack" style="gap:8px">
          <div class="section-title" style="margin-top:6px">1) Группировка по валюте и типу</div>
          <div v-for="g in reportServer.groups" :key="g.currency + ':' + g.type" class="panel">
            <div class="hstack" style="gap:10px">
              <strong>{{ g.type }}</strong>
              <span class="badge">{{ g.currency || '—' }}</span>
              <span class="badge">Итого: {{ Number(g.total).toFixed(2) }} {{ g.currency }}</span>
            </div>
            <div class="hr"></div>
            <table class="table">
              <thead>
                <tr>
                  <th>Дата</th>
                  <th class="right">Сумма</th>
                  <th>Кошелёк</th>
                  <th>Описание</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="x in g.items" :key="x.id">
                  <td>{{ fmtDate(x.date) }}</td>
                  <td class="right">{{ Number(x.amount).toFixed(2) }} {{ x.currency }}</td>
                  <td>{{ x.walletName }}</td>
                  <td>{{ x.description || '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="section-title" style="margin-top:6px">2) Топ-3 расходов по кошелькам</div>
          <div class="grid cards">
            <div v-for="b in reportServer.topByWallet" :key="b.wallet.id" class="panel card">
              <div class="card-head">
                <strong>{{ b.wallet.name }}</strong>
                <span class="badge">Баланс: {{ Number(b.wallet.currentBalance).toFixed(2) }} {{ b.wallet.currency
                  }}</span>
              </div>
              <div class="card-body">
                <div v-if="!b.top.length" class="small">Нет расходов.</div>
                <ol v-else class="report-list">
                  <li v-for="t in b.top" :key="t.id">
                    {{ fmtDate(t.date) }} — {{ Number(t.amount).toFixed(2) }} {{ b.wallet.currency }} — {{ t.description
                    || '—' }}
                  </li>
                </ol>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else class="small">Нет данных отчёта.</div>
    </section>
  </div>
</template>

<style scoped>
.right { margin-left: auto; text-align: right; }

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 44px;
  padding: 8px 14px;
  border-radius: 10px;
  border: 1px solid transparent;
  font-weight: 600;
  line-height: 1.15;
  text-align: center;
  white-space: normal;
  text-wrap: balance;
  cursor: pointer;
}
.btn--block { width: 100%; }
.btn--primary { background: #3b82f6; color: #fff; border-color: #3b82f6; }
.btn--primary:hover { opacity: .92; }
.btn--ghost { background: #fff; color: #111; border-color: #e5e7eb; }
.btn--ghost:hover { background: #f8fafc; }

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
  align-items: center;
}

.wallet-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.wallet-name { font-size: 1.05rem; }
.wallet-toggle { margin-left: auto; }

.filters-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 10px;
}
.align-end { align-items: end; }

.grid.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 12px;
  align-items: stretch;
}

.grid.cards > .panel {
  margin: 0 !important;
  display: flex;
  flex-direction: column;
  height: 100%;
}
.grid.cards > .panel.card {
  margin: 0 !important;
}
.card-head { display:flex; gap:10px; align-items:center; margin-bottom:8px; }
.card-body { display:flex; flex-direction:column; flex:1 1 auto; }
.report-list { margin:0; padding-left:18px; overflow-wrap:anywhere; word-break:break-word; line-height:1.3; }

.table td, .table th { overflow-wrap: anywhere; }

.small.muted { opacity: .7; }
</style>
