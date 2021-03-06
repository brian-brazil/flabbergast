package flabbergast;

import java.util.concurrent.atomic.AtomicInteger;

public class ParseDouble extends Computation implements ConsumeResult {

	private AtomicInteger interlock = new AtomicInteger();
	private String input;

	private TaskMaster master;
	private SourceReference source_reference;
	private Context context;

	public ParseDouble(TaskMaster master, SourceReference source_ref,
			Context context, Frame self, Frame container) {
		this.master = master;
		this.source_reference = source_ref;
		this.context = context;
	}
	@Override
	public void consume(Object result) {
		if (result instanceof Stringish) {
			input = result.toString();
			if (interlock.decrementAndGet() == 0) {
				master.slot(this);
			}
		} else {
			master.reportOtherError(source_reference,
					"Input argument must be a string.");
		}
	}

	@Override
	protected void run() {
		if (input == null) {
			interlock.set(2);

			Computation input_lookup = new Lookup(master, source_reference,
					new String[]{"arg"}, context);
			input_lookup.listen(this);
			master.slot(input_lookup);

			if (interlock.decrementAndGet() > 0) {
				return;
			}
		}

		try {
			result = Double.parseDouble(input);
		} catch (NumberFormatException e) {
			master.reportOtherError(source_reference,
					String.format("Invalid integer “%s”.", input));
		}
	}
}
