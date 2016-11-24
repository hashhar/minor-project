function plotResults  
	[labels,y]=textread('Results.csv', '%s%d', 'delimiter', ',');

	n=length(y);
	x=[1:n]';
	cmap = fliplr(hsv(100));
	clf;
	hold on;
	  for i = 1:n   
		yValue=int16(y(i,:));
		faceColor='r';
		if yValue!=0
			faceColor=cmap(yValue,:);
		end
	   barh(x(i:i),y(i:i),"facecolor", faceColor,.25);
	  end
	   set(gca, 'YTick', 1:n, 'YTickLabel', labels);
	   axis();
	   grid on;
     avg = sum(y) / n;
	   ylabel ("Confusion Sets");
	   xlabel (["Accuracy %","\nAverage: ", num2str(avg)]);
end