import { useState } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { useStrings } from '../../../i18n'
import type { ScheduleRequest, ScheduleResponse } from '../../../lib/api-client'
import { useScheduleCrud, useSchedules } from '../useSchedules'

const NEEDS_WEEKDAYS = new Set(['Weekly', 'Biweekly', 'Custom'])
const NEEDS_MONTH_DAYS = new Set(['Monthly', 'Custom'])

function WeekdayPicker({ value, onChange }: { value: number[]; onChange: (value: number[]) => void }) {
  const strings = useStrings()

  function toggle(day: number) {
    onChange(value.includes(day) ? value.filter((d) => d !== day) : [...value, day].sort())
  }

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-medium text-fg">{strings.wizard.schedule.weekdays}</span>
      <div className="flex flex-wrap gap-1">
        {strings.wizard.schedule.weekdayNames.map((label, day) => (
          <button
            key={day}
            type="button"
            onClick={() => toggle(day)}
            className={`rounded-jm border px-2 py-1 text-xs
              ${value.includes(day) ? 'border-brand bg-brand text-white' : 'border-border-subtle text-fg-muted'}`}
          >
            {label.slice(0, 3)}
          </button>
        ))}
      </div>
    </div>
  )
}

function MonthDayPicker({ value, onChange }: { value: number[]; onChange: (value: number[]) => void }) {
  const strings = useStrings()

  function toggle(day: number) {
    onChange(value.includes(day) ? value.filter((d) => d !== day) : [...value, day].sort((a, b) => a - b))
  }

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-medium text-fg">{strings.wizard.schedule.monthDays}</span>
      <div className="flex flex-wrap gap-1">
        {Array.from({ length: 31 }, (_, index) => index + 1).map((day) => (
          <button
            key={day}
            type="button"
            onClick={() => toggle(day)}
            className={`h-7 w-7 rounded-jm border text-xs
              ${value.includes(day) ? 'border-brand bg-brand text-white' : 'border-border-subtle text-fg-muted'}`}
          >
            {day}
          </button>
        ))}
      </div>
    </div>
  )
}

function TimeListEditor({ value, onChange }: { value: string[]; onChange: (value: string[]) => void }) {
  const strings = useStrings()

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-medium text-fg">{strings.wizard.schedule.times}</span>
      <div className="flex flex-wrap items-center gap-2">
        {value.map((time, index) => (
          <div key={index} className="flex items-center gap-1">
            <Input
              type="time"
              value={time}
              onChange={(event) => onChange(value.map((t, i) => (i === index ? event.target.value : t)))}
            />
            <button
              type="button"
              className="text-xs text-danger"
              onClick={() => onChange(value.filter((_, i) => i !== index))}
            >
              ✕
            </button>
          </div>
        ))}
        <Button size="sm" variant="ghost" onClick={() => onChange([...value, '09:00'])}>
          {strings.wizard.schedule.addTime}
        </Button>
      </div>
    </div>
  )
}

interface ScheduleEditorProps {
  initial?: ScheduleResponse
  onSave: (request: ScheduleRequest) => void
  onRemove?: () => void
  onCancel?: () => void
}

function ScheduleEditor({ initial, onSave, onRemove, onCancel }: ScheduleEditorProps) {
  const strings = useStrings()
  const [frequency, setFrequency] = useState(initial?.frequency ?? 'Daily')
  const [weekdays, setWeekdays] = useState<number[]>(initial?.weekdays ?? [])
  const [monthDays, setMonthDays] = useState<number[]>(initial?.monthDays ?? [])
  const [times, setTimes] = useState<string[]>(initial?.times ?? ['09:00'])

  function handleSave() {
    onSave({
      frequency,
      weekdays: NEEDS_WEEKDAYS.has(frequency) ? weekdays : undefined,
      monthDays: NEEDS_MONTH_DAYS.has(frequency) ? monthDays : undefined,
      times,
    })
  }

  return (
    <div className="flex flex-col gap-3 rounded-jm border border-border-subtle p-3">
      <Select
        label={strings.wizard.schedule.frequency}
        value={frequency}
        onValueChange={setFrequency}
        options={[
          { value: 'Daily', label: strings.wizard.schedule.frequencyDaily },
          { value: 'Weekly', label: strings.wizard.schedule.frequencyWeekly },
          { value: 'Biweekly', label: strings.wizard.schedule.frequencyBiweekly },
          { value: 'Monthly', label: strings.wizard.schedule.frequencyMonthly },
          { value: 'Custom', label: strings.wizard.schedule.frequencyCustom },
        ]}
      />
      {NEEDS_WEEKDAYS.has(frequency) ? <WeekdayPicker value={weekdays} onChange={setWeekdays} /> : null}
      {NEEDS_MONTH_DAYS.has(frequency) ? <MonthDayPicker value={monthDays} onChange={setMonthDays} /> : null}
      <TimeListEditor value={times} onChange={setTimes} />
      <div className="flex gap-2">
        <Button variant="primary" size="sm" onClick={handleSave}>
          {strings.wizard.save}
        </Button>
        {onRemove ? (
          <Button variant="danger" size="sm" onClick={onRemove}>
            {strings.wizard.remove}
          </Button>
        ) : null}
        {onCancel ? (
          <Button variant="ghost" size="sm" onClick={onCancel}>
            {strings.common.cancel}
          </Button>
        ) : null}
      </div>
    </div>
  )
}

export function ScheduleTab({ taskId }: { taskId: number }) {
  const strings = useStrings()
  const schedulesQuery = useSchedules(taskId)
  const { add, update, remove } = useScheduleCrud(taskId)
  const [addingNew, setAddingNew] = useState(false)

  const schedules = schedulesQuery.data ?? []

  return (
    <div className="flex flex-col gap-3">
      {schedules.length === 0 && !addingNew ? <p className="text-sm text-fg-muted">{strings.wizard.schedule.empty}</p> : null}

      {schedules.map((schedule) => (
        <ScheduleEditor
          key={schedule.id}
          initial={schedule}
          onSave={(request) => void update({ scheduleId: schedule.id, request })}
          onRemove={() => void remove(schedule.id)}
        />
      ))}

      {addingNew ? (
        <ScheduleEditor
          onSave={(request) => {
            void add(request)
            setAddingNew(false)
          }}
          onCancel={() => setAddingNew(false)}
        />
      ) : (
        <div>
          <Button onClick={() => setAddingNew(true)}>{strings.wizard.schedule.addSchedule}</Button>
        </div>
      )}
    </div>
  )
}
